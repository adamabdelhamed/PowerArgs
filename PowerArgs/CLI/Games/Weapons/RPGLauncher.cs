using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public interface IMultiPlayerWeapon
    {
        void RemoteFire(MultiPlayerMessage message);
    }
    public class RPGLauncher : Weapon, IMultiPlayerWeapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal()
        {

            var angle = CalculateAngleToTarget();

            if (Holder.MultiPlayerClient != null && Holder is MainCharacter)
            {
                Holder.MultiPlayerClient.TrySendMessage(
                    new RPGFireMessage() { X = Holder.Left, Y = Holder.Top, Angle = angle });
            }

            FireDoubleInternal(Holder.Left, Holder.Top, angle);
        }

        private void FireDoubleInternal(float x, float y, float angle) // :)
        {
            Sound.Play("thump");
            var rpg = new TimedMine(TimeSpan.FromSeconds(2)) { Silent = true };
            rpg.MoveTo(x, y);
            var rpgSpeed = new SpeedTracker(rpg);
            rpgSpeed.HitDetectionTypes.AddRange(this.HitDetectionTypes);
            rpgSpeed.HitDetectionTypes.Add(typeof(Wall));
            rpgSpeed.HitDetectionTypes.Add(typeof(Character));
            rpgSpeed.HitDetectionExclusions.Add(Holder);
            rpgSpeed.ImpactOccurred.SubscribeForLifetime((impact) =>
            {
                if (impact.ElementHit is IDestructible)
                {
                    var destructible = impact.ElementHit as IDestructible;
                    destructible.TakeDamage(5 * rpg.HealthPointsPerShrapnel);
                }

                rpg.Explode();
            }, rpg.Lifetime);

            new Force(rpgSpeed, SpaceExtensions.NormalizeQuantity(25, angle), angle);
            SpaceTime.CurrentSpaceTime.Add(rpg);
        }

        public void RemoteFire(MultiPlayerMessage message)
        {
            var rpgMessage = message as RPGFireMessage;
            FireDoubleInternal(rpgMessage.X, rpgMessage.Y, rpgMessage.Angle);
        }
    }


}
