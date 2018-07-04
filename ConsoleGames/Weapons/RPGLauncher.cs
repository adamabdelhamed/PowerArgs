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
            Sound.Play("thump");
            var rpg = new TimedMine(TimeSpan.FromSeconds(2));
            rpg.MoveTo(Holder.Left, Holder.Top);
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

            var angle = Holder.Target != null ?
                Holder.CalculateAngleTo(MainCharacter.Current.Target) :
                Holder.Speed.Angle;

            if (MainCharacter.Current?.FreeAimCursor != null)
            {
                angle = MainCharacter.Current.CalculateAngleTo(MainCharacter.Current.FreeAimCursor);
            }

            new Force(rpgSpeed, 25, angle);
            SpaceTime.CurrentSpaceTime.Add(rpg);
        }
    }
}
