using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class RemoteMineDropper : Weapon
    {
        RemoteMine activeMine;
        public override void Fire()
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
        public override void Fire()
        {
            var mine = new TimedMine(TimeSpan.FromSeconds(2), MainCharacter.Current.Bounds.Clone(), 5, 4) { HealthPointsPerShrapnel = 5 };
            MainCharacter.Current.Realm.Add(mine);
        }
    }

    public class GrenadeThrower : Weapon
    {
        public override void Fire()
        {
            var mine = new TimedMine(TimeSpan.FromSeconds(2), MainCharacter.Current.Bounds.Clone(), 5, 4) { HealthPointsPerShrapnel = 5 };

            var mineSpeed = new SpeedTracker(mine);
            mineSpeed.HitDetectionTypes.Add(typeof(Wall));
            var angle = MainCharacter.Current.Target != null ?
                MainCharacter.Current.Bounds.Location.CalculateAngleTo(MainCharacter.Current.Target.Bounds.Location) :
                MainCharacter.Current.Speed.Angle;

            new Force(mineSpeed, 10, angle);
            new Force(mineSpeed,5, RealmHelpers.GetOppositeAngle(angle), TimeSpan.FromSeconds(2));
            MainCharacter.Current.Realm.Add(mine);
        }
    }
}
