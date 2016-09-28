using System;

namespace PowerArgs.Cli.Physics
{
    public struct Location
    {
        public float X;
        public float Y;

        public Location(float x, float y)
            : this()
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Location) == false) return false;
            var other = (Location)obj;
            return X == other.X && Y == other.Y;
        }

        public float CalculateDistanceTo(Location other)
        {
            return (float)Math.Sqrt(((X - other.X) * (X - other.X)) + ((Y - other.Y) * (Y - other.Y)));
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", X, Y);
        }

        internal float CalculateAngleTo(Location otherLocation)
        {
            float dx = otherLocation.X - X;
            float dy = otherLocation.Y - Y;
            float d = CalculateDistanceTo(otherLocation);

            if (dy == 0 && dx > 0) return 0;
            else if (dy == 0) return 180;
            else if (dx == 0 && dy > 0) return 90;
            else if (dx == 0) return 270;

            double radians, increment;
            if (dx >= 0 && dy >= 0)
            {
                // Sin(a) = dy / d
                radians = Math.Asin(dy / d);
                increment = 0;

            }
            else if (dx < 0 && dy > 0)
            {
                // Sin(a) = dx / d
                radians = Math.Asin(-dx / d);
                increment = 90;
            }
            else if (dy < 0 && dx < 0)
            {
                radians = Math.Asin(-dy / d);
                increment = 180;
            }
            else if (dx > 0 && dy < 0)
            {
                radians = Math.Asin(dx / d);
                increment = 270;
            }
            else
            {
                throw new Exception();
            }

            return (float)(increment + radians * 180 / Math.PI);
        }
    }
}