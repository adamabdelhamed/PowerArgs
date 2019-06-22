using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class ProximityMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;
        public List<Type> ExcludedTypes { get; set; }

        public override void FireInternal()
        {
            var mine = new ProximityMine() { ExcludedTypes = ExcludedTypes };
            PlaceMineSafe(mine, Holder);
            SpaceTime.CurrentSpaceTime.Add(mine);
        }

        public static void PlaceMineSafe(SpacialElement mine, Character holder)
        {
            var buffer = 2f;
            if (holder.Speed.Angle >= 315 || holder.Speed.Angle < 45)
            {
                mine.MoveTo(holder.Left - buffer * mine.Width, holder.Top, holder.ZIndex);
            }
            else if (holder.Speed.Angle < 135)
            {
                mine.MoveTo(holder.Left, holder.Top - buffer * mine.Height, holder.ZIndex);
            }
            else if (holder.Speed.Angle < 225)
            {
                mine.MoveTo(holder.Left + buffer * mine.Width, holder.Top, holder.ZIndex);
            }
            else
            {
                mine.MoveTo(holder.Left, holder.Top + buffer * mine.Height, holder.ZIndex);
            }
        }
    }
}
