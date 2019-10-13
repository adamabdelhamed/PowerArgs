using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public class SpeedTracker : SpacialElementFunction
    {
        public Event<Impact> ImpactOccurred { get; private set; } = new Event<Impact>();


#if DEBUG

        float _debugOnly_speedX, _debugOnly_speedY;
        public float SpeedX
        {
            get
            {
                return _debugOnly_speedX;
            }
            set
            {
                if (float.IsNaN(value) || float.IsNegativeInfinity(value) || float.IsPositiveInfinity(value) ||
                    float.IsInfinity(value))
                {
                    throw new Exception("Someone is trying to set an invalid speed.  You have a bug :(");
                }
                _debugOnly_speedX = value;
            }
        }

        public float SpeedY
        {
            get
            {
                return _debugOnly_speedY;
            }
            set
            {
                if (float.IsNaN(value) || float.IsNegativeInfinity(value) || float.IsPositiveInfinity(value) ||
                    float.IsInfinity(value))
                {
                    throw new Exception("Someone is trying to set an invalid speed.  You have a bug :(");
                }
                _debugOnly_speedY = value;
            }
        }
#else
        public float SpeedX { get; set; }
        public float SpeedY { get; set; }
#endif

        public float Bounciness { get; set; } // Should be set between 0 and 1
        public float ImpactFriction { get; set; } // Should be set between 0 and 1

        public List<SpacialElement> HitDetectionExclusions { get; private set; } = new List<SpacialElement>();
        public List<Type> HitDetectionExclusionTypes { get; private set; } = new List<Type>();

        private float _angle;
        public float Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
                Element.SizeOrPositionChanged.Fire();
            }
        }

        bool haveMovedSinceLastHitDetection = true;
        public float Speed
        {
            get
            {
                var ret = (float)Math.Sqrt(SpeedX * SpeedX + SpeedY * SpeedY);
                if (float.IsNaN(ret)) throw new Exception();
                return ret;
            }
        }

        public IEnumerable<IRectangularF> GetObstacles() => Element.GetObstacles(HitDetectionExclusions);

        public SpeedTracker(SpacialElement t) : base(t)
        {
            Bounciness = .4f;
            ImpactFriction = .95f;
        }

        public void Stop()
        {
            SpeedX = 0;
            SpeedY = 0;
        }

        public override void Evaluate()
        {
            float dt = (float)Governor.Rate.TotalSeconds;
            if (dt == 0) dt = (float)Time.CurrentTime.Increment.TotalSeconds;

            float dx = SpeedX * dt;
            float dy = SpeedY * dt;

            var obstacles = GetObstacles().ToList();

            if (dx == 0 && dy == 0)
            {
                return;
            }

            var effectiveExclusions = HitDetectionExclusionTypes.Count > 0 || HitDetectionExclusions.Count == 0 ?
                new List<IRectangularF>(this.HitDetectionExclusions.Union(SpaceTime.CurrentSpaceTime.Elements.Where(e => HitDetectionExclusionTypes.Contains(e.GetType())))) : null;
            var hitPrediction = HitDetection.PredictHit(new HitDetectionOptions()
            {
                Bounds = SpaceTime.CurrentSpaceTime.Bounds,
                MovingObject = Element,
                Exclusions = effectiveExclusions,
                Obstacles = obstacles.As<IRectangularF>().ToList(),
                Dx = dx,
                Dy = dy,
            });

            if (hitPrediction.Type != HitType.None)
            {
                float angle  = Element.Center().CalculateAngleTo(hitPrediction.ObstacleHit.Center());

                if (haveMovedSinceLastHitDetection)
                {
                    ImpactOccurred?.Fire(new Impact()
                    {
                        Angle = angle,
                        MovingObject = Element,
                        ObstacleHit = hitPrediction.ObstacleHit,
                        HitType = hitPrediction.Type,
                    });

                    haveMovedSinceLastHitDetection = false;
                    var testArea = RectangularF.Create(Element.Left + dx, Element.Top + dy, Element.Width, Element.Height);

                    if (hitPrediction.Direction == Direction.Down || hitPrediction.Direction == Direction.Up)
                    {
                        SpeedY = -SpeedY * Bounciness;
                        SpeedX = SpeedX * ImpactFriction;
                    }
                    else if (hitPrediction.Direction == Direction.Left || hitPrediction.Direction == Direction.Right)
                    {
                        SpeedX = -SpeedX * Bounciness;
                        SpeedY = SpeedY * ImpactFriction;
                    }
                    else
                    {
                        SpeedX = -SpeedX * Bounciness;
                        SpeedY = -SpeedY * Bounciness;
                    }
                    Element.SizeOrPositionChanged.Fire();
                }
            }
            else
            {
                var oldLocation = Element.CopyBounds();
                Element.MoveBy(dx, dy);

                this.Angle = oldLocation.Center().CalculateAngleTo(Element.Center());
                haveMovedSinceLastHitDetection = true;
            }
        }

        internal static void FindEdgesGivenHyp(float hyp, float angle, out float dx, out float dy)
        {
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
