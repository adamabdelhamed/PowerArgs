using System;
using System.Collections.Generic;

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
        public List<Type> HitDetectionTypes { get; set; }

        public float Angle { get; private set; }

        bool haveMovedSinceLastHitDetection;
        public float Speed
        {
            get
            {
                var ret = (float)Math.Sqrt(SpeedX * SpeedX + SpeedY * SpeedY);
                if (float.IsNaN(ret)) throw new Exception();
                return ret;
            }
        }

        public SpeedTracker(SpacialElement t) : base(t)
        {
            HitDetectionTypes = new List<Type>();
            Bounciness = .4f;
            ImpactFriction = .95f;
        }
        public override void Initialize()
        {

        }

        public override void Evaluate()
        {
            float dt = (float)Governor.Rate.TotalSeconds;
            float dx = SpeedX * dt;
            float dy = SpeedY * dt;

            var hitPrediction = HitDetection.PredictHit(SpaceTime.CurrentSpaceTime, Element, HitDetectionTypes, dx, dy);

            if (hitPrediction.Type != HitType.None)
            {
                float angle = Element.Center().CalculateAngleTo(hitPrediction.ElementHit.Center());
                if (ImpactOccurred != null && haveMovedSinceLastHitDetection)
                {
                    ImpactOccurred.Fire(new Impact()
                    {
                        Angle = angle,
                        Bounds = hitPrediction.BoundsOfItemBeingHit,
                        ElementHit = hitPrediction.ElementHit
                    });
                }
                haveMovedSinceLastHitDetection = false;
                var testArea = Rectangular.Create(Element.Left + dx, Element.Top + dy, Element.Width, Element.Height);

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

        private static void FindDeltas(float angle, float hyp, out float adj, out float opp)
        {
            float radians = 3.1415926535897932f * angle / 180.0f;
            opp = (float)(hyp * Math.Sin(radians));
            adj = (float)(Math.Sqrt((hyp * hyp) - (opp * opp)));
        }
    }
}
