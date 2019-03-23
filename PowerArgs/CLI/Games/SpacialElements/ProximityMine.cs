using PowerArgs.Cli.Physics;
using System.Linq;
namespace PowerArgs.Games
{
    public class ProximityMine : Explosive
    { 
        public override void Evaluate()
        {
            if(DamageBroker.Instance.DamageableElements.Where(e => e.CalculateDistanceTo(this) <= this.Range/2).Any())
            {
                Explode();
            }
        }
    }
}
