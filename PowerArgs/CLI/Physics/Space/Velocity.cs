using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public class Velocity : SpacialElementFunction
    {
        public Event<Impact> ImpactOccurred { get; private set; } = new Event<Impact>();
        public static Event<Impact> GlobalImpactOccurred { get; private set; } = new Event<Impact>();

        public List<SpacialElement> HitDetectionExclusions { get; private set; } = new List<SpacialElement>();
        public List<Type> HitDetectionExclusionTypes { get; private set; } = new List<Type>();
        public Func<IEnumerable<SpacialElement>> HitDetectionDynamicExclusions { get; set; }
        
        public float Angle { get; set; }

        bool haveMovedSinceLastHitDetection = true;
        public float Speed { get; set; }

        public List<IRectangularF> GetObstacles() => Element.GetObstacles(HitDetectionExclusions, HitDetectionExclusionTypes, HitDetectionDynamicExclusions);

        public List<IRectangularF> LastObstacles { get; set; }
        public HitPrediction LastPrediction { get; set; }

        public Velocity(SpacialElement t) : base(t) { }

        public void Stop()
        {
            Speed = 0;
        }

        public override void Evaluate()
        {
            float dt = (float)Governor.Rate.TotalSeconds;
            if (dt == 0) dt = (float)Time.CurrentTime.Increment.TotalSeconds;
            float d = Speed * dt;

            if (d == 0)
            {
                return;
            }

            var obstacles = GetObstacles().ToList();
            LastObstacles = obstacles;
            var hitPrediction = HitDetection.PredictHit(new HitDetectionOptions()
            {
                MovingObject = Element is IHaveMassBounds ? (Element as IHaveMassBounds).MassBounds : Element,
                Obstacles = obstacles.As<IRectangularF>().ToList(),
                Angle = Angle,
                Visibility = d,
            });
            LastPrediction = hitPrediction;
            if (hitPrediction.Type != HitType.None)
            {
                if(hitPrediction.LKG != null && Element.TopLeft().Equals(hitPrediction.LKG) == false)
                {
                    Element.MoveTo(hitPrediction.LKG.Left, hitPrediction.LKG.Top);
                    haveMovedSinceLastHitDetection = true;
                }

                float angle = Element.Center().CalculateAngleTo(hitPrediction.ObstacleHit.Center());


                if (haveMovedSinceLastHitDetection)
                {
                    var impact = new Impact()
                    {
                        Angle = angle,
                        MovingObject = Element,
                        ObstacleHit = hitPrediction.ObstacleHit,
                        HitType = hitPrediction.Type,
                    };
                    ImpactOccurred?.Fire(impact);
                    GlobalImpactOccurred.Fire(impact);

                    haveMovedSinceLastHitDetection = false;
                    Element.SizeOrPositionChanged.Fire();
                }
            }
            else
            {
                var newLocation = Element.MoveTowards(Angle, d);
                Element.MoveTo(newLocation.Left, newLocation.Top);
                haveMovedSinceLastHitDetection = true;
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
