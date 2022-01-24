using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public interface IHaveVelocity : ISpacialElement
    {
        Velocity Velocity { get; }
    }

    public static class IHaveVelocityEx
    {
        public static ILifetimeManager CreateNextVelocityChangedLifetime(this IHaveVelocity el)
        {
            var lt = Lifetime.EarliestOf(el.Velocity.OnAngleChanged.CreateNextFireLifetime(), el.Velocity.OnSpeedChanged.CreateNextFireLifetime());
            return lt;
        }
    }

    public class Velocity : SpacialElementFunction
    {
        public Event OnVelocityEnforced { get; private set; } = new Event();
        public Event<Impact> ImpactOccurred { get; private set; } = new Event<Impact>();
        public static Event<Impact> GlobalImpactOccurred { get; private set; } = new Event<Impact>();
       
        public List<SpacialElement> HitDetectionExclusions { get; private set; } = new List<SpacialElement>();
        public List<Type> HitDetectionExclusionTypes { get; private set; } = new List<Type>();
        public Func<IEnumerable<SpacialElement>> HitDetectionDynamicExclusions { get; set; }

        public Func<IRectangularF> BoundsTransform { get; set; }

        public Event OnAngleChanged { get; private set; } = new Event();
        public Event OnSpeedChanged { get; private set; } = new Event();
        public Event BeforeMove { get; private set; } = new Event();

        private float angle;
        public float Angle
        {
            get
            {
                return angle;
            }
            set
            {
                if (value == angle) return;
                angle = value;
                OnAngleChanged.Fire();
            }
        }

        bool haveMovedSinceLastHitDetection = true;

        private float speed;
        public float Speed
        {
            get
            {
                return speed;
            }
            set
            {
                if (value == speed) return;
                speed = value;
                OnSpeedChanged.Fire();
            }
        }

        public List<IRectangularF> GetObstacles() => Element.GetObstacles();

        public HitPrediction NextCollision { get; set; }

        public TimeSpan NextCollisionETA
        {
            get
            {
                if (NextCollision == null || Speed == 0 || NextCollision.ObstacleHit == null) return TimeSpan.MaxValue;
                var d = NextCollision.LKGD;
                var seconds = d / speed;
                return TimeSpan.FromSeconds(seconds);
            }
        }

        public Impact LastImpact { get; private set; }

        public bool Bounce { get; set; }

        public bool HitDetectionDisabled { get; set; }

        [ThreadStatic]
        private static bool isEvaluating;
        public Velocity(SpacialElement t) : base(t)
        {
            Added.SubscribeOnce(() =>
            {
                if(t is IHaveVelocity == false)
                {
                    dynamicVelocities.Add(t, this);
                    this.Lifetime.OnDisposed(()=> dynamicVelocities.Remove(t));
                    t.Lifetime.OnDisposed(() => dynamicVelocities.Remove(t));
                }

                if(isEvaluating == false)
                {
                    isEvaluating = true;
                    ExecuteAsync();
                }
            });
        }

        private static Dictionary<SpacialElement, Velocity> dynamicVelocities = new Dictionary<SpacialElement, Velocity>();
        public static Velocity For(SpacialElement el)
        {
            if(el is IHaveVelocity)
            {
                return (el as IHaveVelocity).Velocity;
            }
            else if(dynamicVelocities.TryGetValue(el, out Velocity v))
            {
                return v;
            }
            else
            {
                return null;
            }
        }

        public void Stop()
        {
            Speed = 0;
        }

        private static async void ExecuteAsync()
        {
            while (Time.CurrentTime.IsExpired == false)
            {
                await Task.Yield();
                float dt = (float)Time.CurrentTime.Increment.TotalSeconds;
                if (dt == 0) dt = (float)Time.CurrentTime.Increment.TotalSeconds;

                foreach (var velocity in Time.CurrentTime.Functions.WhereAs<Velocity>())
                {
                    if (velocity.Lifetime.IsExpired) continue;
                    float d = velocity.Speed * dt;

                    if (d == 0)
                    {
                        velocity.BeforeMove.Fire();
                        velocity.OnVelocityEnforced?.Fire();
                        continue;
                    }

                    HitPrediction hitPrediction = null;
                    IRectangularF bounds = null;
                    if (velocity.HitDetectionDisabled == false)
                    {
                        var obstacles = velocity.GetObstacles();

                        bounds = velocity.BoundsTransform != null ? velocity.BoundsTransform() : velocity.Element;
                        hitPrediction = HitDetection.PredictHit(new HitDetectionOptions()
                        {
                            MovingObject = bounds,
                            Obstacles = obstacles,
                            Angle = velocity.Angle,
                            Visibility = SpaceTime.CurrentSpaceTime.Bounds.Hypotenous(),
                            Mode = CastingMode.Precise,
                        });
                        velocity.NextCollision = hitPrediction;
                        velocity.BeforeMove.Fire();
                    }


                    if (hitPrediction != null && hitPrediction.Type != HitType.None && hitPrediction.LKGD <= d)
                    {
                        var dx = velocity.BoundsTransform != null ? bounds.Left - velocity.Element.Left : 0;
                        var dy = velocity.BoundsTransform != null ? bounds.Top - velocity.Element.Top : 0;

                        var proposedBounds = velocity.BoundsTransform != null ? velocity.BoundsTransform() : velocity.Element;
                        var distanceToObstacleHit = proposedBounds.CalculateDistanceTo(hitPrediction.ObstacleHit);
                        if (distanceToObstacleHit > .5f)
                        {
                            proposedBounds = proposedBounds.MoveTowards(velocity.Angle, distanceToObstacleHit - .5f, false);
                            velocity.Element.MoveTo(proposedBounds.Left - dx, proposedBounds.Top - dy);
                            velocity.haveMovedSinceLastHitDetection = true;
                        }
                        float angle = bounds.Center().CalculateAngleTo(hitPrediction.ObstacleHit.Center());


                        if (velocity.haveMovedSinceLastHitDetection)
                        {
                            velocity.LastImpact = new Impact()
                            {
                                Angle = angle,
                                MovingObject = velocity.Element,
                                ObstacleHit = hitPrediction.ObstacleHit,
                                HitType = hitPrediction.Type,
                                Prediction = hitPrediction,
                            };

                            if (hitPrediction.ObstacleHit is SpacialElement)
                            {
                                Velocity.For(hitPrediction.ObstacleHit as SpacialElement)?.ImpactOccurred.Fire(new Impact()
                                {
                                    Angle = angle.GetOppositeAngle(),
                                    MovingObject = hitPrediction.ObstacleHit as SpacialElement,
                                    ObstacleHit = velocity.Element,
                                    HitType = hitPrediction.Type,
                                });
                            }

                            velocity.ImpactOccurred?.Fire(velocity.LastImpact);
                            GlobalImpactOccurred.Fire(velocity.LastImpact);

                            velocity.haveMovedSinceLastHitDetection = false;
                            velocity.Element.SizeOrPositionChanged.Fire();
                        }

                        if (velocity.Bounce)
                        {
                            if (hitPrediction.Side == Side.Top || hitPrediction.Side == Side.Bottom)
                            {
                                velocity.Angle = 0.AddToAngle(-velocity.Angle);
                            }
                            else
                            {
                                velocity.Angle = 180.AddToAngle(-velocity.Angle);
                            }
                        }
                        else
                        {
                            velocity.Stop();
                        }
                    }
                    else
                    {
                        var newLocation = velocity.Element.MoveTowards(velocity.Angle, d);
                        velocity.Element.MoveTo(newLocation.Left, newLocation.Top);
                        velocity.haveMovedSinceLastHitDetection = true;
                    }

                    velocity.OnVelocityEnforced?.Fire();
                }
            }
        }

    }
}
