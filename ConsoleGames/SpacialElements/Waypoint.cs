using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleGames
{
    public class Waypoint : SpacialElement
    {
        public bool IsHighlighted { get; private set; }

        private static Dictionary<Character, Waypoint> mostRecentlyVisited = new Dictionary<Character, Waypoint>();

        public static void MoveTowards(Character character, IRectangular destination, float increment)
        {
            if(mostRecentlyVisited.ContainsKey(character) == false)
            {
                mostRecentlyVisited.Add(character, null);
                character.Lifetime.LifetimeManager.Manage(() => { mostRecentlyVisited.Remove(character); });
            }

            if(character.CalculateLineOfSight(destination, increment).Obstacles.Where(o => o is Waypoint == false).Count() == 0)
            {
                var newTopLeft = character.TopLeft().MoveTowards(destination.Center(), increment);
                character.MoveTo(newTopLeft.Left, newTopLeft.Top);
                return;
            }

            var bestWaypoint = SpaceTime.CurrentSpaceTime.Elements
                .Where(e => e is Waypoint)
                .Select(e => e as Waypoint)
                .Where(w => w != mostRecentlyVisited[character])
                .Where(w => character.CalculateLineOfSight(w, increment).Obstacles.Where(o => o is Waypoint == false).Count() == 0)
                .OrderBy(w => w.CalculateLineOfSight(destination, increment).Obstacles.Where(o => o is Waypoint == false).Count() == 0 ? 0 : 1)
                .ThenBy(w => w.CalculateDistanceTo(destination))
                .ThenBy(w => character.CalculateDistanceTo(w))
                .FirstOrDefault();
            
            if(bestWaypoint != null)
            {
                var newTopLeft = character.TopLeft().MoveTowards(bestWaypoint.Center(), increment);
                character.MoveTo(newTopLeft.Left, newTopLeft.Top);

                if(character.CalculateDistanceTo(bestWaypoint) <= 1.5)
                {
                    mostRecentlyVisited[character] = bestWaypoint;
                }

            }
        }
    }

    [SpacialElementBinding(typeof(Waypoint))]
    public class WaypointRenderer : SpacialElementRenderer
    {
        public WaypointRenderer()
        {
            this.TransparentBackground = true;
            CanFocus = false;
            ZIndex = 10;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if ((Element as Waypoint).IsHighlighted)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('W', backgroundColor: ConsoleColor.Cyan);
                context.FillRect(0, 0, Width, Height);
            }
        }
    }

    public class WaypointReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            var waypointTag = item.Tags.Where(t => t.Equals("waypoint")).SingleOrDefault();
            if (waypointTag == null)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new Waypoint();
            return true;
        }
    }
}
