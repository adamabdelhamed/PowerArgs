using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
namespace PowerArgs.Games
{
    public class Pistol : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;
        public override void FireInternal()
        {
            var bullet = new Projectile(Holder.Left, Holder.Top, CalculateAngleToTarget()) { PlaySoundOnImpact = true };
            SpaceTime.CurrentSpaceTime.Add(bullet);
            Sound.Play("pistol");
        }
    }
}
