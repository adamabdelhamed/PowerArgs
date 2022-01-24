using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public enum HitType
    {
        None = 0,
        Obstacle = 1,
    }

    public class Impact
    {
        public float Angle { get; set; }
        public SpacialElement MovingObject { get; set; }
        public IRectangularF ObstacleHit { get; set; }
        public HitType HitType { get; set; }
        public HitPrediction Prediction { get; set; }
    }

    public class HitPrediction
    {
        public HitType Type { get; set; }
        public Direction Direction { get; set; }
        public IRectangularF ObstacleHit { get; set; }
        public float LKGX { get; set; }
        public float LKGY { get; set; }
        public float LKGD { get; set; }
        public float Visibility { get; set; }
        public bool ElementWasAlreadyObstructed { get; set; }

        public Side Side { get; set; }
        public Edge Edge { get; set; }

        public float IntersectionX { get; set; }
        public float IntersectionY { get; set; }

        public ILocationF Intersection => LocationF.Create(IntersectionX, IntersectionY);
    }

    public class HitDetectionOptions
    {
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Angle { get; set; }
        public float Visibility { get; set; } 

        public CastingMode Mode { get; set; } = CastingMode.Precise;

        public List<Edge> EdgesHitOutput { get; set; }
    }

    public enum CastingMode
    {
        SingleRay,
        Rough,
        Precise
    }

    public static class HitDetection
    {
        public static bool HasLineOfSight(this Velocity from, IRectangularF to) => HasLineOfSight(from.Element, to, from.GetObstacles());
        public static bool HasLineOfSight(this SpacialElement from, IRectangularF to) => HasLineOfSight(from, to, from.GetObstacles());
        public static bool HasLineOfSight(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles) => GetLineOfSightObstruction(from, to, obstacles) == null;

        public static IRectangularF GetLineOfSightObstruction(this IRectangularF from, IRectangularF to, IEnumerable<IRectangularF> obstacles, CastingMode castingMode = CastingMode.Rough)
        {
            var prediction = PredictHit(new HitDetectionOptions()
            {
                MovingObject = from,
                Angle = from.Center().CalculateAngleTo(to.Center()),
                Obstacles = obstacles.Union(new IRectangularF[] { to }),
                Visibility = 3 * from.Center().CalculateDistanceTo(to.Center()),
                Mode = castingMode,
            });

            if (prediction.Type == HitType.None)
            {
                return null;
            }
            else
            {
                if (to is IHaveMassBounds && prediction.ObstacleHit is SpacialElement && (to as IHaveMassBounds).IsPartOfMass(prediction.ObstacleHit as SpacialElement))
                {
                    return null;
                }

                if (prediction.ObstacleHit is SpacialElement && (to is IHaveMassBounds) && (to as IHaveMassBounds).IsPartOfMass((SpacialElement)prediction.ObstacleHit))
                {
                    return null;
                }
                else
                {
                    return prediction.ObstacleHit == to ? null : prediction.ObstacleHit;
                }
            }
        }

        [ThreadStatic]
        private static Edge[] castBuffer;

        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            return PredictHit(options.MovingObject, options.Obstacles.ToArray(), options.Angle, options.Visibility, options.Mode, options.EdgesHitOutput);
        }

        public static HitPrediction PredictHit(IRectangularF movingObject, IRectangularF[] obstacles, float angle, float visibility = 10000f, CastingMode mode = CastingMode.Precise, List<Edge> edgesHitOutput = null)
        {
            HitPrediction prediction = new HitPrediction();
            prediction.LKGX = movingObject.Left;
            prediction.LKGY = movingObject.Top;


            prediction.Visibility = visibility;
            if (visibility == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }


            var mov = movingObject;

            var rayIndex = 0;
            castBuffer = castBuffer ??  new Edge[10000];
            if (mode == CastingMode.Precise)
            {
                var delta = SpacialAwareness.MoveTowardsFast(mov.Left, mov.Top, angle, visibility, normalized: false);
                var dx = delta.Item1 - mov.Left;
                var dy = delta.Item2 - mov.Top;

                castBuffer[rayIndex++] = new Edge() { X1 = mov.Left, Y1 = mov.Top, X2 = mov.Left + dx, Y2 = mov.Top + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.Right(), Y1 = mov.Top, X2 = mov.Right() + dx, Y2 = mov.Top + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.Left, Y1 = mov.Bottom(), X2 = mov.Left + dx, Y2 = mov.Bottom() + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.Right(), Y1 = mov.Bottom(), X2 = mov.Right() + dx, Y2 = mov.Bottom() + dy };
                 

                var granularity = .5f;

                for (var x = mov.Left + granularity; x < mov.Left + mov.Width; x += granularity)
                {
                    castBuffer[rayIndex++] = new Edge() { X1 = x, Y1 = mov.Top, X2 = x+dx, Y2 = mov.Top+dy };
                    castBuffer[rayIndex++] = new Edge() { X1 = x, Y1 = mov.Bottom(), X2 = x + dx, Y2 = mov.Bottom() + dy };
                }

                for (var y = mov.Top + granularity; y < mov.Top + mov.Height; y += granularity)
                {
                    castBuffer[rayIndex++] = new Edge() { X1 = mov.Left, Y1 = y, X2 = mov.Left+dx, Y2 = y+dy };
                    castBuffer[rayIndex++] = new Edge() { X1 = mov.Right(), Y1 = y, X2 = mov.Right() + dx, Y2 = y + dy };
                }
            }
            else if (mode == CastingMode.Rough)
            {
                var delta = SpacialAwareness.MoveTowardsFast(mov.Left, mov.Top, angle, visibility, normalized: false);
                var dx = delta.Item1 - mov.Left;
                var dy = delta.Item2 - mov.Top;

                castBuffer[rayIndex++] = new Edge() { X1 = mov.Left, Y1 = mov.Top, X2 = mov.Left + dx, Y2 = mov.Top + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.Right(), Y1 = mov.Top, X2 = mov.Right() + dx, Y2 = mov.Top + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.Left, Y1 = mov.Bottom(), X2 = mov.Left + dx, Y2 = mov.Bottom() + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.Right(), Y1 = mov.Bottom(), X2 = mov.Right() + dx, Y2 = mov.Bottom() + dy };
                castBuffer[rayIndex++] = new Edge() { X1 = mov.CenterX(), Y1 = mov.CenterY(), X2 = mov.CenterX() + dx, Y2 = mov.CenterY() + dy };
            }
            else if(mode == CastingMode.SingleRay)
            {
                var delta = SpacialAwareness.MoveTowardsFast(mov.Left, mov.Top, angle, visibility, normalized: false);
                var dx = delta.Item1 - mov.Left;
                var dy = delta.Item2 - mov.Top;

                castBuffer[rayIndex++] = new Edge() { X1 = mov.CenterX(), Y1 = mov.CenterY(), X2 = mov.CenterX() + dx, Y2 = mov.CenterY() + dy };
            }
            else
            {
                throw new NotSupportedException("Unknown mode: "+mode);
            }

            var closestIntersectionDistance = float.MaxValue;
            IRectangularF closestIntersectingElement = null;
            Edge closestEdge = default;
            Side closestSide = default;
            float closestIntersectionX = 0;
            float closestIntersectionY = 0;
            for (var i = 0; i < obstacles.Length; i++)
            {
                var obstacle = obstacles[i];
                ProcessEdge(obstacle.TopEdge, Side.Top, rayIndex, edgesHitOutput, visibility, prediction, ref closestIntersectionDistance, ref closestIntersectingElement, ref closestEdge, ref closestSide, ref closestIntersectionX, ref closestIntersectionY, obstacle);
                ProcessEdge(obstacle.BottomEdge, Side.Bottom, rayIndex, edgesHitOutput, visibility, prediction, ref closestIntersectionDistance, ref closestIntersectingElement, ref closestEdge, ref closestSide, ref closestIntersectionX, ref closestIntersectionY, obstacle);
                ProcessEdge(obstacle.LeftEdge, Side.Left, rayIndex, edgesHitOutput, visibility, prediction, ref closestIntersectionDistance, ref closestIntersectingElement, ref closestEdge, ref closestSide, ref closestIntersectionX, ref closestIntersectionY, obstacle);
                ProcessEdge(obstacle.RightEdge, Side.Right, rayIndex, edgesHitOutput, visibility, prediction, ref closestIntersectionDistance, ref closestIntersectingElement, ref closestEdge, ref closestSide, ref closestIntersectionX, ref closestIntersectionY, obstacle);

            }

            if(closestIntersectingElement != null)
            {
                prediction.ObstacleHit = closestIntersectingElement;
                prediction.LKGD = closestIntersectionDistance - .1f;

                var lkg = SpacialAwareness.MoveTowardsFast(movingObject.Left, movingObject.Top, angle, prediction.LKGD, normalized: false);
                prediction.LKGX = lkg.Item1;
                prediction.LKGY = lkg.Item2;
                prediction.Type = HitType.Obstacle;
                prediction.Edge = closestEdge;
                prediction.Side = closestSide;
                prediction.IntersectionX = closestIntersectionX;
                prediction.IntersectionY = closestIntersectionY;
            }

            return prediction;
        }

        private static void ProcessEdge(Edge edge, Side side, int castLength, List<Edge> edgesHitOutput, float visibility, HitPrediction prediction, ref float closestIntersectionDistance, ref IRectangularF closestIntersectingElement, ref Edge closestEdge, ref Side closestSide, ref float closestIntersectionX, ref float closestIntersectionY, IRectangularF obstacle)
        {
            for (var k = 0; k < castLength; k++)
            {
                var ray = castBuffer[k];
                var success = TryFindIntersectionPoint(ray, edge, out float ix, out float iy);
                if (success)
                {
                    edgesHitOutput?.Add(ray);
                    var d = Geometry.CalculateDistanceTo(ray.X1, ray.Y1, ix, iy);

                    if (d < closestIntersectionDistance && d <= visibility)
                    {
                        closestIntersectionDistance = d;
                        closestIntersectingElement = obstacle;
                        closestEdge = edge;
                        closestSide = side;
                        closestIntersectionX = ix;
                        closestIntersectionY = iy;
                    }
                }
            }
        }

        private static bool TryFindIntersectionPoint(Edge a, Edge b, out float x, out float y)
        {
            var x1 = a.X1;
            var y1 = a.Y1;
            var x2 = a.X2;
            var y2 = a.Y2;

            var x3 = b.X1;
            var y3 = b.Y1;
            var x4 = b.X2;
            var y4 = b.Y2;

            var den = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (den == 0)
            {
                x = 0;
                y = 0;
                return false;
            }

            var t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / den;
            if(t <= 0 || t >= 1)
            {
                x = 0;
                y = 0;
                return false;
            }

            var u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / den;
            if (u > 0 && u < 1)
            {
                x = x1 + t * (x2 - x1);
                y = y1 + t * (y2 - y1);
                return true;
            }
            else
            {
                x = 0;
                y = 0;
                return false;
            }
        }
    }
}