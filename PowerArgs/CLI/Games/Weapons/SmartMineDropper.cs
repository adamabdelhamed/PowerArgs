using PowerArgs.Cli.Physics;
namespace PowerArgs.Games
{
    public class SmartMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;
        public string TargetTag { get; set; } = "enemy";

        public float Speed { get; set; } = 50;

        public override void FireInternal(bool alt)
        {
            var mine = new ProximityMine(this) { TargetTag = TargetTag };
            mine.SetProperty(nameof(Holder), this.Holder);
            ProximityMineDropper.PlaceMineSafe(mine, Holder, !alt, Speed);
            SpaceTime.CurrentSpaceTime.Add(mine);
            OnWeaponElementEmitted.Fire(mine);
        }
    }
}
