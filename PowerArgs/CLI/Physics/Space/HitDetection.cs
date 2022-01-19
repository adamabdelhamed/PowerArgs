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
        public IRectangularF MovingObjectPosition { get; set; }
        public HitType Type { get; set; }
        public Direction Direction { get; set; }
        public IRectangularF ObstacleHit { get; set; }
        public ILocationF LKG { get; set; }
        public float LKGD { get; set; }
        public float Visibility { get; set; }
        public bool ElementWasAlreadyObstructed { get; set; }

        public int EdgeIndex { get; set; }
        public ILocationF Intersection { get; set; }
        public List<Edge> RaysCast { get; set; }
        public List<Edge> RaysHit { get; set; }
    }

    public class HitDetectionOptions
    {
        public IRectangularF MovingObject { get; set; }
        public IEnumerable<IRectangularF> Obstacles { get; set; }
        public float Angle { get; set; }
        public float Visibility { get; set; } 

        public CastingMode Mode { get; set; } = CastingMode.Precise;
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



        public static HitPrediction PredictHit(HitDetectionOptions options)
        {
            HitPrediction prediction = new HitPrediction();
            prediction.LKG = options.MovingObject.CopyBounds().TopLeft();
            prediction.MovingObjectPosition = options.MovingObject.CopyBounds();
            prediction.Visibility = options.Visibility;
            if (options.Visibility == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
                return prediction;
            }


            var mov = options.MovingObject;

            prediction.RaysHit = new List<Edge>();
            if (options.Mode == CastingMode.Precise)
            {
                var tl = mov.TopLeft();
                var delta = tl.MoveTowards(options.Angle, options.Visibility, normalized: false);
                var dx = delta.Left - tl.Left;
                var dy = delta.Top - tl.Top;
                prediction.RaysCast = new List<Edge>()
                {
                    new Edge() { X1 = mov.Left, Y1 = mov.Top, X2 = mov.Left + dx, Y2 = mov.Top + dy},
                    new Edge() { X1 = mov.Right(), Y1 = mov.Top, X2 = mov.Right()+dx, Y2 = mov.Top+dy },
                    new Edge() { X1 = mov.Left, Y1 = mov.Bottom(), X2 = mov.Left+dx, Y2 = mov.Bottom()+dy },
                    new Edge() { X1 = mov.Right(), Y1 = mov.Bottom(), X2 = mov.Right()+dx, Y2 = mov.Bottom()+dy },
                };

                var granularity = .5f;

                for (var x = mov.Left + granularity; x < mov.Left + mov.Width; x += granularity)
                {
                    prediction.RaysCast.Add(new Edge() { X1 = x, Y1 = mov.Top, X2 = x+dx, Y2 = mov.Top+dy });
                    prediction.RaysCast.Add(new Edge() { X1 = x, Y1 = mov.Bottom(), X2 = x + dx, Y2 = mov.Bottom() + dy });
                }

                for (var y = mov.Top + granularity; y < mov.Top + mov.Height; y += granularity)
                {
                    prediction.RaysCast.Add(new Edge() { X1 = mov.Left, Y1 = y, X2 = mov.Left+dx, Y2 = y+dy });
                    prediction.RaysCast.Add(new Edge() { X1 = mov.Right(), Y1 = y, X2 = mov.Right() + dx, Y2 = y + dy });
                }
            }
            else if (options.Mode == CastingMode.Rough)
            {
                var tl = mov.TopLeft();
                var delta = tl.MoveTowards(options.Angle, options.Visibility, normalized: false);
                var dx = delta.Left - tl.Left;
                var dy = delta.Top - tl.Top;

                var center = options.MovingObject.Center();
                prediction.RaysCast = new List<Edge>() 
                {
                    new Edge() { X1 = mov.Left, Y1 = mov.Top, X2 = mov.Left + dx, Y2 = mov.Top + dy},
                    new Edge() { X1 = mov.Right(), Y1 = mov.Top, X2 = mov.Right()+dx, Y2 = mov.Top+dy },
                    new Edge() { X1 = mov.Left, Y1 = mov.Bottom(), X2 = mov.Left+dx, Y2 = mov.Bottom()+dy },
                    new Edge() { X1 = mov.Right(), Y1 = mov.Bottom(), X2 = mov.Right()+dx, Y2 = mov.Bottom()+dy },
                    new Edge() { X1 = mov.CenterX(), Y1 = mov.CenterY(), X2 = mov.CenterX()+dx, Y2 = mov.CenterY()+dy },
                };
            }
            else if(options.Mode == CastingMode.SingleRay)
            {
                var tl = mov.TopLeft();
                var delta = tl.MoveTowards(options.Angle, options.Visibility, normalized: false);
                var dx = delta.Left - tl.Left;
                var dy = delta.Top - tl.Top;
                prediction.RaysCast = new List<Edge>()
                {
                    new Edge() { X1 = mov.CenterX(), Y1 = mov.CenterY(), X2 = mov.CenterX()+dx, Y2 = mov.CenterY()+dy },
                };
            }
            else
            {
                throw new NotSupportedException("Unknown mode: "+options.Mode);
            }

            var closestIntersectionDistance = float.MaxValue;
            IRectangularF closestIntersectingElement = null;
            var closestEdgeIndex = -1;
            float closestIntersectionX = 0;
            float closestIntersectionY = 0;
            var effectiveObstacles = options.Obstacles.ToArray();
            for (var i = 0; i < effectiveObstacles.Length; i++)
            {
                var obstacle = effectiveObstacles[i];
                for(var j = 0; j < obstacle.Edges.Length; j++)
                {
                    var edge = obstacle.Edges[j];
                    for(var k = 0; k < prediction.RaysCast.Count; k++)
                    {
                        var ray = prediction.RaysCast[k];
                        var success = TryFindIntersectionPoint(ray, edge, out float ix, out float iy);
                        if (success)
                        {
                            prediction.RaysHit.Add(ray);
                            var d = Geometry.CalculateDistanceTo(ray.X1, ray.Y1, ix, iy);
 
                            if (d < closestIntersectionDistance && d <= options.Visibility)
                            {
                                closestIntersectionDistance = d;
                                closestIntersectingElement = obstacle;
                                closestEdgeIndex = j;
                                closestIntersectionX = ix;
                                closestIntersectionY = iy;
                            }
                        }
                    }
                }
            }

            if(closestIntersectingElement != null)
            {
                prediction.ObstacleHit = closestIntersectingElement;
                prediction.LKGD = closestIntersectionDistance - .1f;
                prediction.LKG = options.MovingObject.MoveTowards(options.Angle, prediction.LKGD, normalized:false).TopLeft();
                prediction.Type = HitType.Obstacle;
                prediction.EdgeIndex = closestEdgeIndex;
                prediction.Intersection = LocationF.Create(closestIntersectionX, closestIntersectionY);
            }

            return prediction;
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