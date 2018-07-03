using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGames
{
    public class RPGLauncher : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {
            // todo - uncomment when we get sound
            //SoundEffects.Instance.PlaySound("thump");
            var rpg = new TimedMine(TimeSpan.FromSeconds(2), MainCharacter.Current.Left, MainCharacter.Current.Top, 5, 4) { HealthPointsPerShrapnel = 5 };

            var rpgSpeed = new SpeedTracker(rpg);
            rpgSpeed.HitDetectionTypes.Add(typeof(Wall));
            rpgSpeed.HitDetectionTypes.Add(typeof(Enemy));
            rpgSpeed.ImpactOccurred.SubscribeForLifetime((impact) =>
            {
                if (impact.ElementHit is IDestructible)
                {
                    var destructible = impact.ElementHit as IDestructible;
                    destructible.TakeDamage(5 * rpg.HealthPointsPerShrapnel);
                }

                rpg.Explode();
            }, rpg.Lifetime);

            var angle = MainCharacter.Current.Target != null ?
                MainCharacter.Current.CalculateAngleTo(MainCharacter.Current.Target) :
                MainCharacter.Current.Speed.Angle;

            if (MainCharacter.Current.FreeAimCursor != null)
            {
                angle = MainCharacter.Current.CalculateAngleTo(MainCharacter.Current.FreeAimCursor);
            }

            new Force(rpgSpeed, 10, angle);
            SpaceTime.CurrentSpaceTime.Add(rpg);
        }
    }
}
