using PowerArgs.Cli.Physics;
using System.Linq;
namespace PowerArgs.Games
{
    public class ProximityMine : Explosive
    { 
        public override void Evaluate()
        {
            if(SpaceTime.CurrentSpaceTime.Elements.Where(e => e is Enemy && e.CalculateDistanceTo(this) <= this.Range/2).Count() > 0)
            {
                Explode();
            }
        }
    }
}
