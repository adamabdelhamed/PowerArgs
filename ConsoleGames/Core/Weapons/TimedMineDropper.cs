using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class TimedMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {
            var mine = new TimedMine(TimeSpan.FromSeconds(2), MainCharacter.Current.Left, MainCharacter.Current.Top, 5, 4) { HealthPointsPerShrapnel = 5 };
            SpaceTime.CurrentSpaceTime.Add(mine);
        }
    }
}
