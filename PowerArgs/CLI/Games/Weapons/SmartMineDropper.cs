using PowerArgs.Cli.Physics;
namespace PowerArgs.Games
{
    public class SmartMineDropper : Weapon
    {
        public float Range { get; set; } = 10;
        public float AngleIncrement { get; set; } = 30;
        public override WeaponStyle Style => WeaponStyle.Explosive;
        public string TargetTag { get; set; } = "enemy";

        public float Speed { get; set; } = 50;

        public override void FireInternal(bool alt)
        {
            var mine = new ProximityMine(this) { TargetTag = TargetTag, Range = Range, AngleIncrement = AngleIncrement };
            mine.SetProperty(nameof(Holder), this.Holder);
            ProximityMineDropper.PlaceMineSafe(mine, Holder, !alt, Speed);
            SpaceTime.CurrentSpaceTime.Add(mine);
            OnWeaponElementEmitted.Fire(mine);
        }
    }
}
