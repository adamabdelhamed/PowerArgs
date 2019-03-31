using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
namespace PowerArgs.Games
{
    public class Pistol : Weapon
    {
        public ConsoleString ProjectilePen { get; set; }
        public override WeaponStyle Style => WeaponStyle.Primary;
        public override void FireInternal()
        {
            
            var bullet = new Projectile(Holder.Left, Holder.Top, CalculateAngleToTarget()) { PlaySoundOnImpact = true };

            if(ProjectilePen != null)
            {
                bullet.Pen = ProjectilePen;
            }

            SpaceTime.CurrentSpaceTime.Add(bullet);
            Sound.Play("pistol");
        }
    }
}
