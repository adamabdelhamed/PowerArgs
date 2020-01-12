using PowerArgs.Cli.Physics;
namespace PowerArgs.Games
{
    public class Pistol : Weapon
    {
        public float Accelleration { get; set; } = 50;

        public ConsoleString ProjectilePen { get; set; }
        public override WeaponStyle Style => WeaponStyle.Primary;
        public override void FireInternal()
        {
            var bullet = new Projectile(this,Holder.CenterX() - Projectile.StandardWidth/ 2, Holder.CenterY() - Projectile.StandardHeight / 2, CalculateAngleToTarget()) { PlaySoundOnImpact = true };
            bullet.Accelleration = Accelleration;
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
        }
    }
}
