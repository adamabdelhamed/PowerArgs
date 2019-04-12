using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public static class SpacialAwareness
    {
        public static bool HasLineOfSight(this IRectangularF from, IRectangularF to, List<IRectangularF> obstacles, float increment)
        {
            IRectangularF current = from;
            var currentDistance = current.CalculateDistanceTo(to);
            var firstDistance = currentDistance;
            while (currentDistance > increment)
            {
                current = RectangularF.Create(MoveTowards(current.Center(), to.Center(), 1), current);
                current = RectangularF.Create(current.Left - current.Width / 2, current.Top - current.Height / 2, current.Width, current.Height);

                foreach (var obstacle in obstacles)
                {
                    if(obstacle == to || obstacle == from)
                    {
                        continue;
                    }
                    else if (obstacle.OverlapPercentage(current) > 0)
                    {
                        return false;
                    }
                }

                currentDistance = current.CalculateDistanceTo(to);
            }

            return true;
        }
        
        public static ILocationF MoveTowards(this ILocationF a, ILocationF b, float distance)
        {
            float slope = (a.Top - b.Top) / (a.Left - b.Left);
            bool forward = a.Left <= b.Left;
            bool up = a.Top <= b.Top;

            float abDistance = a.CalculateDistanceTo(b);
            double angle = Math.Asin(Math.Abs(b.Top - a.Top) / abDistance);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top + dy : a.Top - dy;

            var ret = LocationF.Create(x2, y2);
            return ret;
        }

        public static ILocationF MoveTowards(this ILocationF a, float angle, float distance)
        {
            distance = Geometry.NormalizeQuantity(distance, angle, reverse: true);
            var forward = angle > 270 || angle < 90;
            var up = angle > 180;

            // convert to radians
            angle = (float)(angle * Math.PI / 180);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top - dy : a.Top + dy;

            var ret = LocationF.Create(x2, y2);
            return ret;
        }

        public static IEnumerable<SpacialElement> GetObstacles(this SpacialElement element, IEnumerable<SpacialElement> exclusions = null) => SpaceTime.CurrentSpaceTime
                   .Elements
                   .Where(e => e == element == false &&
                           (exclusions == null ||  exclusions.Contains(e) == false) &&
                           e.ZIndex == element.ZIndex &&
                           e.HasSimpleTag("passthru") == false);
        


        public static bool TryNudgeFreeOFObstacles(this SpacialElement element, List<IRectangularF> obstacles, float range = 3.5f, int skip = 0)
        {
            var minX = element.Left - range / 2;
            var maxX = element.Left + range / 2;
            var numX = maxX - minX;

            var minY = element.Top - range / 2;
            var maxY = element.Top + range / 2;
            var numY = maxY - minY;

            var n = (int)Math.Floor(numX * numY);
            skip = skip % n;
            for (var x = minX; x < maxX; x++)
            {
                for (var y = minY; y < maxY; y++)
                {
                    if(skip > 0)
                    {
                        skip--;
                        continue;
                    }

                    var testArea = RectangularF.Create(x, y, element.Width * 1.1f, element.Height * 1.1f);
                    if(obstacles.Where(o => o.Touches(testArea)).Any())
                    {
                        continue;
                    }
                    else
                    {
                        element.MoveTo(x, y);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
