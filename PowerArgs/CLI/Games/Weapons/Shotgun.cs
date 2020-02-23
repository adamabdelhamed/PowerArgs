using PowerArgs.Cli.Physics;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class Shotgun : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;
        public ConsoleString ProjectilePen { get; set; }
        public float Speed { get; set; } = 70f;
        public float Range { get; set; } = 15f;

        public override void FireInternal(bool alt)
        {
            var targetAngle = Holder.CalculateAngleToTarget();
            var sprayAngle = 30.0f;
            var sprayIncrement = 5;
            var startAngle = targetAngle.AddToAngle(-sprayAngle/2);
            var sprayedSoFar = 0;

            var bullets = new List<Projectile>();
            while (sprayedSoFar <= sprayAngle)
            {
                var angle = startAngle.AddToAngle(sprayedSoFar);
                var bullet = new Projectile(this, Speed, angle) { Range = Range.NormalizeQuantity(angle), PlaySoundOnImpact = true };
                bullet.Velocity.HitDetectionExclusions.Add(Holder);
                Holder.Velocity.HitDetectionExclusions.Add(bullet);
                bullet.Lifetime.OnDisposed(() =>
                {
                    Holder.Velocity.HitDetectionExclusions.Remove(bullet);
                });
                bullet.MoveTo(bullet.Left, bullet.Top, Holder.ZIndex);
                if (ProjectilePen != null)
                {
                    bullet.Pen = ProjectilePen;
                }

                bullets.Add(bullet);
                OnWeaponElementEmitted.Fire(bullet);
                sprayedSoFar += sprayIncrement;
            }

            // n squared so keep n small or else pay the price!!!
            foreach(var bullet in bullets)
            {
                foreach(var innerBullet in bullets)
                {
                    if (innerBullet != bullet)
                    {
                        bullet.Velocity.HitDetectionExclusions.Add(innerBullet);
                    }
                }
            }

            foreach(var bullet in bullets)
            {
                SpaceTime.CurrentSpaceTime.Add(bullet);
            }
        }
    }
}
