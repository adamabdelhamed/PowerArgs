using PowerArgs.Cli.Physics;
namespace PowerArgs.Games
{
    public class Pistol : Weapon
    {
        public float Speed { get; set; } = 70;

        public ConsoleString ProjectilePen { get; set; }
        public override WeaponStyle Style => WeaponStyle.Primary;
        public override void FireInternal(bool alt)
        {
            var bullet = new Projectile(this, Speed, Holder.CalculateAngleToTarget()) { PlaySoundOnImpact = true };
            bullet.Velocity.HitDetectionExclusions.Add(Holder);
            bullet.Velocity.HitDetectionExclusions.AddRange(Holder.Velocity.HitDetectionExclusions);
            bullet.Velocity.HitDetectionExclusionTypes.AddRange(Holder.Velocity.HitDetectionExclusionTypes);
            Holder.Velocity.HitDetectionExclusions.Add(bullet);
            bullet.Lifetime.OnDisposed(()=> Holder.Velocity.HitDetectionExclusions.Remove(bullet));
            bullet.MoveTo(bullet.Left, bullet.Top, Holder.ZIndex);
            if(ProjectilePen != null)
            {
                bullet.Pen = ProjectilePen;
            }

            SpaceTime.CurrentSpaceTime.Add(bullet);
            OnWeaponElementEmitted.Fire(bullet);
        }
    }
}
