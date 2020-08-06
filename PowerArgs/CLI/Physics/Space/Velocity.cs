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

    public static class VelocityEx
    {
        public static async Task<bool> TryControlVelocity(this SpacialElement el, Func<Velocity, Task> takeoverAction, ILifetimeManager lt)
        {
            Velocity tempV = new Velocity(el);
            lt.OnDisposed(tempV.Lifetime.Dispose);
            if (el is IHaveVelocity)
            {
                if ((el as IHaveVelocity).Velocity.MovementTakeover != null)
                {
                    return false;
                }
                tempV.HitDetectionDynamicExclusions = (el as IHaveVelocity).Velocity.HitDetectionDynamicExclusions;
                tempV.HitDetectionExclusions.AddRange((el as IHaveVelocity).Velocity.HitDetectionExclusions);
                tempV.HitDetectionExclusionTypes.AddRange((el as IHaveVelocity).Velocity.HitDetectionExclusionTypes);
                await (el as IHaveVelocity).Velocity.Takeover(() => takeoverAction(tempV));
            }
            else
            {
                await takeoverAction(tempV);
            }
            return true;
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

        public HitPrediction LastPrediction { get; set; }

        public Impact LastImpact { get; private set; }

        public Func<Task> MovementTakeover { get; private set; }

        public Velocity(SpacialElement t) : base(t)
        {
            Time.CurrentTime.Invoke(async()=> await ExecuteAsync());
        }

        public static Velocity For(SpacialElement el)
        {
            if(el is IHaveVelocity)
            {
                return (el as IHaveVelocity).Velocity;
            }
            else
            {
                return Time.CurrentTime.Functions.WhereAs<Velocity>().Where(v => v.Element == el).FirstOrDefault();
            }
        }

        public Task Takeover(Func<Task> movementTakeover)
        {
            Deferred d = Deferred.Create();
            if (MovementTakeover != null) throw new ArgumentException("Movement has already been taken over");
            MovementTakeover = async () =>
            {
                await movementTakeover();
                MovementTakeover = null;
                d.Resolve();
            };
            return d.Promise.AsAwaitable();
        }

        public void Stop()
        {
            Speed = 0;
        }


        private async Task ExecuteAsync()
        {
            while (this.Lifetime.IsExpired == false)
            {
                await Time.CurrentTime.YieldAsync();
                if (this.Lifetime.IsExpired) return;
                if (MovementTakeover != null)
                {
                    await MovementTakeover();
                    continue;
                }

                float dt = (float)Time.CurrentTime.Increment.TotalSeconds;
                if (dt == 0) dt = (float)Time.CurrentTime.Increment.TotalSeconds;
                float d = Speed * dt;

                if (d == 0)
                {
                    OnVelocityEnforced?.Fire();
                    continue;
                }

                var obstacles = GetObstacles();

                var bounds = BoundsTransform != null ? BoundsTransform() : Element;
                var dx = BoundsTransform != null ? bounds.Left - Element.Left : 0;
                var dy = BoundsTransform != null ? bounds.Top - Element.Top : 0;

                var hitPrediction = HitDetection.PredictHit(new HitDetectionOptions()
                {
                    MovingObject = bounds,
                    Obstacles = obstacles,
                    Angle = Angle,
                    Visibility = d,
                });
                LastPrediction = hitPrediction;
                if (hitPrediction.Type != HitType.None)
                {
                    var proposedBounds = BoundsTransform != null ? BoundsTransform() : Element;
                    var distanceToObstacleHit = proposedBounds.CalculateDistanceTo(hitPrediction.ObstacleHit);
                    if (distanceToObstacleHit > .5f)
                    {
                        proposedBounds = proposedBounds.MoveTowards(Angle, distanceToObstacleHit-.5f, false);
                    }

                    var dx2 = BoundsTransform != null ? proposedBounds.Left - Element.Left : 0;
                    var dy2 = BoundsTransform != null ? proposedBounds.Top - Element.Top : 0;
                    Element.MoveTo(proposedBounds.Left - dx, proposedBounds.Top - dy);

                    haveMovedSinceLastHitDetection = true;
                    float angle = bounds.Center().CalculateAngleTo(hitPrediction.ObstacleHit.Center());


                    if (haveMovedSinceLastHitDetection)
                    {
                        LastImpact = new Impact()
                        {
                            Angle = angle,
                            MovingObject = Element,
                            ObstacleHit = hitPrediction.ObstacleHit,
                            HitType = hitPrediction.Type,
                        };

                        if (hitPrediction.ObstacleHit is SpacialElement)
                        {
                            Velocity.For(hitPrediction.ObstacleHit as SpacialElement)?.ImpactOccurred.Fire(new Impact()
                            {
                                Angle = angle.GetOppositeAngle(),
                                MovingObject = hitPrediction.ObstacleHit as SpacialElement,
                                ObstacleHit = Element,
                                HitType = hitPrediction.Type,
                            });
                        }

                        ImpactOccurred?.Fire(LastImpact);
                        GlobalImpactOccurred.Fire(LastImpact);

                        haveMovedSinceLastHitDetection = false;
                        Element.SizeOrPositionChanged.Fire();
                    }

                    Stop();
                }
                else
                {
                    var newLocation = Element.MoveTowards(Angle, d);
                    Element.MoveTo(newLocation.Left, newLocation.Top);
                    haveMovedSinceLastHitDetection = true;
                }

                OnVelocityEnforced?.Fire();
            }
        }

        internal static void FindEdgesGivenHyp(float hyp, float angle, out float dx, out float dy)
        {
            if(angle == 360)
            {
                angle = 0;
            }
            float angleTemp, d1, d2;
            if (angle >= 0 && angle < 90)
            {
                angleTemp = angle;
                FindDeltas(angleTemp, hyp, out d1, out d2);
                dx = d1;
                dy = d2;
            }
            else if (angle >= 90 && angle < 180)
            {
                angleTemp = angle - 90;
                FindDeltas(angleTemp, hyp, out d1, out d2);
                dx = -d2;
                dy = d1;
            }
            else if (angle >= 180 && angle < 270)
            {
                angleTemp = angle - 180;
                FindDeltas(angleTemp, hyp, out d1, out d2);
                dx = -d1;
                dy = -d2;
            }
            else if (angle >= 270 && angle < 360)
            {
                angleTemp = angle - 270;
                FindDeltas(angleTemp, hyp, out d1, out d2);
                dx = d2;
                dy = -d1;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Angle must be >= 0 and < 360");
            }
        }

        public bool IsComingTowards(IRectangularF target)
        {
            var d = Element.CalculateDistanceTo(target);
            var projectedLocation = this.Element.TopLeft().MoveTowards(this.Angle, d);
            var projectedRect = RectangularF.Create(projectedLocation.Left, projectedLocation.Top, Element.Width, Element.Height);
            var ret = projectedRect.CalculateDistanceTo(target);
            return ret < .5;
        }

        private static void FindDeltas(float angle, float hyp, out float adj, out float opp)
        {
            float radians = 3.1415926535897932f * angle / 180.0f;
            opp = (float)(hyp * Math.Sin(radians));
            adj = (float)(Math.Sqrt((hyp * hyp) - (opp * opp)));
        }
    }
}
