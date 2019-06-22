using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs.Games
{
    public enum ProximityMineState
    {
        NoNearbyThreats,
        ThreatApproaching,
        ThreatNearby
    }

    public class ProximityMine : Explosive
    {
        public List<Type> ExcludedTypes { get; set; }

        public ProximityMineState State { get; set; } = ProximityMineState.NoNearbyThreats;

        public override void Evaluate()
        {
            var closestTarget = DamageBroker.Instance.DamageableElements
                .Where(e => e.ZIndex == this.ZIndex)
                .Where(e => IsIncluded(e))
                .Select(t => new { Target = t, Distance = t.CalculateDistanceTo(this) })
                .OrderBy(t => t.Distance)
                .FirstOrDefault();

            if(closestTarget == null)
            {
                State = ProximityMineState.NoNearbyThreats;
            }
            else if (closestTarget.Distance < Range)
            {
                Explode();
            }
            else if(closestTarget.Distance < Range * 3f)
            {
                State = ProximityMineState.ThreatNearby;
            }
            else if(closestTarget.Distance < Range * 6f)
            {
                State = ProximityMineState.ThreatApproaching;
            }
            else
            {
                State = ProximityMineState.NoNearbyThreats;
            }
        }

        private bool IsIncluded(SpacialElement e)
        {
            if (ExcludedTypes == null) return true;
            else return ExcludedTypes.Contains(e.GetType()) == false;
        }
    }

    [SpacialElementBinding(typeof(ProximityMine))]
    public class ProximityMineRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            var state = (Element as ProximityMine).State;

            if(state == ProximityMineState.NoNearbyThreats)
            {
                context.FillRect(new ConsoleCharacter('#', ConsoleColor.DarkGray), 0, 0, Width, Height);
            }
            else if(state == ProximityMineState.ThreatApproaching)
            {
                context.FillRect(new ConsoleCharacter('#', ConsoleColor.Black, ConsoleColor.DarkYellow), 0, 0, Width, Height);
            }
            else if(state == ProximityMineState.ThreatNearby)
            {
                context.FillRect(new ConsoleCharacter('#', ConsoleColor.Black, ConsoleColor.Red), 0, 0, Width, Height);
            }
        }
    }
}
