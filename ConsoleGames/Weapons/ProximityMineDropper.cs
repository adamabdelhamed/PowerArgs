using PowerArgs.Cli.Physics;

namespace ConsoleGames
{
    public class ProximityMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {
            SpaceTime.CurrentSpaceTime.Add(new ProximityMine());
        }
    }
}
