using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
namespace ConsoleGames.Shooter
{
    public class Pistol : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public override void FireInternal()
        {
            var angle = MainCharacter.Current.Target != null ?
                MainCharacter.Current.Bounds.CalculateAngleTo(MainCharacter.Current.Target) :
                MainCharacter.Current.Speed.Angle;

            if (MainCharacter.Current.FreeAimCursor != null)
            {
                angle = MainCharacter.Current.CalculateAngleTo(MainCharacter.Current.FreeAimCursor);

            }

            var bullet = new Projectile(MainCharacter.Current.Left, MainCharacter.Current.Top, angle) { PlaySoundOnImpact = true };
            bullet.Speed.HitDetectionTypes.Remove(typeof(MainCharacter));
            SpaceTime.CurrentSpaceTime.Add(bullet);
            
            // todo - uncomment after sound added
            //SoundEffects.Instance.PlaySound("pistol");
        }
    }

    public class PistolAmmo : LooseAmmo<Pistol>
    {
        public PistolAmmo(int amount) : base(amount) { }
    }

    [SpacialElementBinding(typeof(PistolAmmo))]
    public class PistolAmmoRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new PowerArgs.ConsoleCharacter('P', foregroundColor: ConsoleColor.White, backgroundColor: ConsoleColor.Blue);
            context.DrawPoint(0, 0);
        }
    }
}
