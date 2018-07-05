using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
namespace ConsoleGames
{
    public class Pistol : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public override void FireInternal()
        {   
            var bullet = new Projectile(Holder.Left, Holder.Top, CalculateAngleToTarget()) { PlaySoundOnImpact = true };

            bullet.Speed.HitDetectionTypes.Remove(Holder.GetType());

            if (Holder.Target != null)
            {
                bullet.Speed.HitDetectionTypes.Add(Holder.Target.GetType());
            }
            SpaceTime.CurrentSpaceTime.Add(bullet);

            Sound.Play("pistol");
        }
    }
}
