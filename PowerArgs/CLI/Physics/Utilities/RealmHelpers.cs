using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
namespace PowerArgs.Cli.Physics
{
    public static class RealmHelpers
    {
        public enum HitType
        {
            None = 0,
            Thing = 1,
            Boundary = 2,
        }

        public class HitPrediction
        {
            public HitType Type { get; set; }
            public Direction Direction { get; set; }
            public Rectangle BoundsOfItemBeingHit { get; set; }
            public Thing ThingHit { get; set; }
        }

        static Random random = new Random();

        public static float GetOppositeAngle(float angle)
        {
            float ret;
            if (angle < 180)
            {
                ret = angle + 180;
            }
            else
            {
                ret = angle - 180;
            }

            if (ret == 360) ret = 0;

            return ret;
        }

        public static bool IsOneOfThese(Thing t, List<Type> these)
        {
            Type thingType = t.GetType();

            var count = these.Count;
            for (int i = 0; i < count; i++)
            {
                if (these[i] == thingType || thingType.GetTypeInfo().IsSubclassOf(these[i])) return true;
            }

            return false;
        }

        public static List<Thing> GetThingsThatTouch(Realm r, Thing target)
        {
            List<Thing> ret = new List<Thing>();
            Thing t;
            for (int i = 0; i < r.Things.Count(); i++)
            {
                t = r.Things.ElementAt(i);
                if (t != target && t.Bounds.Hits(target.Bounds))
                {
                    ret.Add(t);
                }
            }
            return ret;
        }

        public static void PlaceInEmptyLocation(Realm r, Thing toPlace)
        {
            float minX = r.Bounds.Location.X;
            float maxX = r.Bounds.Location.X + r.Bounds.Size.W - 3 * toPlace.Bounds.Size.W;
            float rangeX = maxX - minX;

            float minY = r.Bounds.Location.Y;
            float maxY = r.Bounds.Location.Y + r.Bounds.Size.H - 3 * toPlace.Bounds.Size.H;
            float rangeY = maxY - minY;

            while (true)
            {
                float x = minX + (float)(random.NextDouble() * rangeX);
                float y = minY + (float)(random.NextDouble() * rangeY);


                toPlace.Bounds.MoveTo(new Location(x, y));
                if (RealmHelpers.GetThingsITouch(r, toPlace, new List<Type>() { typeof(Thing) }).Count() == 0)
                {
                    r.Update(toPlace);
                    break;
                }
            }
        }

        public static HitPrediction PredictHit(Realm r, Thing Target, List<Type> hitDetectionTypes, float dx, float dy)
        {
            HitPrediction prediction = new HitPrediction();

            if (Target.Bottom + dy >= r.Bounds.Size.H)
            {
                prediction.Direction = Direction.Down;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = new Rectangle(Target.Left + dx, r.Bounds.Size.H + dy, 1, 1);
                return prediction;
            }
            else if (Target.Left + dx <= 0)
            {
                prediction.Direction = Direction.Left;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = new Rectangle(-dx, Target.Top + dy, 1, 1);
                return prediction;
            }
            else if (Target.Top + dy <= 0)
            {
                prediction.Direction = Direction.Up;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = new Rectangle(Target.Left + dx, -dy, 1, 1);
                return prediction;
            }
            else if (Target.Right + dx >= r.Bounds.Size.W)
            {
                prediction.Direction = Direction.Right;
                prediction.Type = HitType.Boundary;
                prediction.BoundsOfItemBeingHit = new Rectangle(r.Bounds.Size.W + dx, Target.Top + dy, 1, 1);
                return prediction;
            }

            var testArea = new Rectangle(Target.Left + dx, Target.Top + dy, Target.Bounds.Size.W, Target.Bounds.Size.H);

            var match = (from t in r.Things
                         where
                             IsOneOfThese(t, hitDetectionTypes) &&
                             Target != t &&
                             testArea.Hits(t.Bounds)
                         select t).OrderBy(t => t.Bounds.Location.CalculateDistanceTo(Target.Bounds.Location));


            if (match.Count() == 0)
            {
                prediction.Direction = Direction.None;
                prediction.Type = HitType.None;
            }
            else
            {
                prediction.ThingHit = match.First();
                prediction.Type = HitType.Thing;
                prediction.Direction = testArea.GetHitDirection(match.First().Bounds);
                prediction.BoundsOfItemBeingHit = prediction.ThingHit.Bounds;
            }

            return prediction;
        }

        public static bool MoveThingSafeBy(Realm r, Thing t, float x, float y, bool suppressChangeEvent = false)
        {
            Location orig = t.Bounds.Location;
            t.Bounds.MoveBy(x, y);
            if (t.Bounds.Location.X < 0) t.Bounds.Location = new Location() { X = 0, Y = t.Bounds.Location.Y };
            if (t.Bounds.Location.Y < 0) t.Bounds.Location = new Location() { X = t.Bounds.Location.X, Y = 0 };

            if (t.Bounds.Location.X + t.Bounds.Size.W > r.Bounds.Size.W) t.Bounds.Location = new Location() { X = r.Bounds.Size.W - t.Bounds.Size.W, Y = t.Bounds.Location.Y };
            if (t.Bounds.Location.Y + t.Bounds.Size.H > r.Bounds.Size.H) t.Bounds.Location = new Location() { X = t.Bounds.Location.X, Y = r.Bounds.Size.H - t.Bounds.Size.H };

            if (orig.Equals(t.Bounds.Location) == false)
            {
                if (!suppressChangeEvent) r.Update(t);
                return true;
            }
            return false;
        }

        public static Location MoveTowards(Location a, Location b, float distance)
        {
            float slope = (a.Y - b.Y) / (a.X - b.X);
            bool forward = a.X <= b.X;
            bool up = a.Y <= b.Y;

            float abDistance = a.CalculateDistanceTo(b);
            double angle = Math.Asin(Math.Abs(b.Y - a.Y) / abDistance);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.X + dx : a.X - dx;
            float y2 = up ? a.Y + dy : a.Y - dy;

            var ret = new Location() { X = x2, Y = y2 };
#if DEBUG
            var distanceCheck = ret.CalculateDistanceTo(a);
            var newDistance = ret.CalculateDistanceTo(b);
            // distanceCheck should be pretty darn close to distance
#endif
            return ret;
        }

        public static Route CalculateLineOfSight(Realm r, Thing from, Location to, float increment)
        {
            Route ret = new Route();
            Rectangle current = from.Bounds.Clone();
            while (current.Location.CalculateDistanceTo(to) > increment)
            {
                current = new Rectangle(MoveTowards(current.Location, to, increment), from.Bounds.Size);
                ret.Steps.Add(current.Location);

                var obstacles = GetThingsThatTouch(r, new Thing(current));

                foreach(var obstacle in obstacles)
                {
                    if(ret.Obstacles.Contains(obstacle) == false)
                    {
                        ret.Obstacles.Add(obstacle);
                    }
                }
            }

            return ret;
        }

        public static bool DoesThingTouchA<T>(Thing target, Realm r, float dx = 0, float dy = 0) where T : Thing
        {
            Rectangle testArea = new Rectangle(target.Bounds.Location.X + dx, target.Bounds.Location.Y + dy, target.Bounds.Size.W, target.Bounds.Size.H);
            var touching = GetThingsThatTouch(r, new Thing(testArea));
            var filtered = from t in touching where t is T || t.GetType().GetTypeInfo().IsSubclassOf(typeof(T)) select t;
            return filtered.Count() > 0;
        }

        public static bool DoesThingTouchAny(Realm r, Thing target, List<Type> thingsToNotHit, float dx = 0, float dy = 0)
        {
            return GetThingsITouch(r, target, thingsToNotHit, dx, dy).Count() > 0;
        }

        public static IEnumerable<Thing> GetThingsITouch(Realm r, Thing target, List<Type> types, float dx = 0, float dy = 0)
        {
            Rectangle testArea = new Rectangle(target.Left + dx, target.Top + dy, target.Bounds.Size.W, target.Bounds.Size.H);
            var matchingThings = from t in r.Things
                                 where IsOneOfThese(t, types) && t != target && t.Bounds.Hits(testArea)
                                 select t;

            return matchingThings;
        }
    }
}
