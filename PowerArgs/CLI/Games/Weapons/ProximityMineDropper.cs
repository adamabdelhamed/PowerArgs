using PowerArgs.Cli.Physics;

namespace PowerArgs.Games
{
    public class ProximityMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;
        public string TargetTag { get; set; }

        public override void FireInternal()
        {
            var mine = new ProximityMine(this) { TargetTag = TargetTag };
            PlaceMineSafe(mine, Holder);
            SpaceTime.CurrentSpaceTime.Add(mine);
        }

        public static void PlaceMineSafe(SpacialElement mine, Character holder)
        {
            var buffer = 2f;
            if (holder.Velocity.Angle >= 315 || holder.Velocity.Angle < 45)
            {
                mine.MoveTo(holder.Left - buffer * mine.Width, holder.Top, holder.ZIndex);
            }
            else if (holder.Velocity.Angle < 135)
            {
                mine.MoveTo(holder.Left, holder.Top - buffer * mine.Height, holder.ZIndex);
            }
            else if (holder.Velocity.Angle < 225)
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
