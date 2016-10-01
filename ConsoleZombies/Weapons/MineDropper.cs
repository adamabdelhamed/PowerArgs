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
}
