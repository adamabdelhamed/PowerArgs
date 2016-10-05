using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class RemoteMineDropper : Weapon
    {
        RemoteMine activeMine;
        public override void FireInternal()
        {
            if (activeMine != null)
            {
                activeMine.Detonate();
                activeMine = null;
            }
            else
            {
                activeMine = new RemoteMine(MainCharacter.Current.Bounds.Clone(), 5, 4) {  HealthPointsPerShrapnel = 5};
                MainCharacter.Current.Realm.Add(activeMine);
            }
        }
    }

    public class TimedMineDropper : Weapon
    {
        public override void FireInternal()
        {
            var mine = new TimedMine(TimeSpan.FromSeconds(2), MainCharacter.Current.Bounds.Clone(), 5, 4) { HealthPointsPerShrapnel = 5 };
            MainCharacter.Current.Realm.Add(mine);
        }
    }

    public class RPGLauncher : Weapon
    {
        public override void FireInternal()
        {
            SoundEffects.Instance.PlaySound("thump");
            var rpg = new TimedMine(TimeSpan.FromSeconds(2), MainCharacter.Current.Bounds.Clone(), 5, 4) { HealthPointsPerShrapnel = 5 };

            var rpgSpeed = new SpeedTracker(rpg);
            rpgSpeed.HitDetectionTypes.Add(typeof(Wall));
            rpgSpeed.HitDetectionTypes.Add(typeof(Zombie));
            rpgSpeed.ImpactOccurred.SubscribeForLifetime((impact)=>
            {
                if(impact.ThingHit is IDestructible)
                {
                    var destructible = impact.ThingHit as IDestructible;
                    destructible.HealthPoints -= 5*rpg.HealthPointsPerShrapnel;
                    if (destructible.HealthPoints <= 0)
                    {
                        if (impact.ThingHit is MainCharacter)
                        {
                            MainCharacter.Current.EatenByZombie.Fire();
                        }
                        impact.ThingHit.Realm.Remove(impact.ThingHit);
                    }
                }

                rpg.Explode();
            },rpg.LifetimeManager);
            var angle = MainCharacter.Current.Target != null ?
                MainCharacter.Current.Bounds.Location.CalculateAngleTo(MainCharacter.Current.Target.Bounds.Location) :
                MainCharacter.Current.Speed.Angle;

            if (MainCharacter.Current.FreeAimCursor != null)
            {
                angle = MainCharacter.Current.Bounds.Location.CalculateAngleTo(MainCharacter.Current.FreeAimCursor.Bounds.Location);
            }

            new Force(rpgSpeed, 30, angle);
            new Force(rpgSpeed,15, RealmHelpers.GetOppositeAngle(angle), TimeSpan.FromSeconds(1));
            MainCharacter.Current.Realm.Add(rpg);
        }
    }
}
