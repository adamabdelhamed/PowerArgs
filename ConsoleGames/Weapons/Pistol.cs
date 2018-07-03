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
            var angle = Holder.Target != null ?
                Holder.Bounds.CalculateAngleTo(Holder.Target) :
                MainCharacter.Current.Speed.Angle;

            if (Holder == MainCharacter.Current && MainCharacter.Current.FreeAimCursor != null)
            {
                angle = Holder.CalculateAngleTo(MainCharacter.Current.FreeAimCursor);
            }

            var bullet = new Projectile(Holder.Left, Holder.Top, angle) { PlaySoundOnImpact = true };

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
