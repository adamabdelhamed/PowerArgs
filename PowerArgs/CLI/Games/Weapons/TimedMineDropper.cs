using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class TimedMineDropper : Weapon
    {
        public Event Exploded { get; private set; } = new Event();

        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(3.5);

        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {
            var mine = new TimedMine(this,Delay);
            ProximityMineDropper.PlaceMineSafe(mine, Holder);
            SpaceTime.CurrentSpaceTime.Add(mine);
            mine.Exploded.SubscribeOnce(this.Exploded.Fire);
        }
    }
}
