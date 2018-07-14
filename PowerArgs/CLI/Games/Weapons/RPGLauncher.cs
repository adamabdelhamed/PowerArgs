using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class RPGLauncher : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {
            Sound.Play("thump");
            var rpg = new TimedMine(TimeSpan.FromSeconds(2)) { Silent = true };
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

            new Force(rpgSpeed, SpaceExtensions.NormalizeQuantity(25, CalculateAngleToTarget()), CalculateAngleToTarget());
            SpaceTime.CurrentSpaceTime.Add(rpg);
        }
    }
}
