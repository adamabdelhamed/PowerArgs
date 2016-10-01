using System;

namespace ConsoleZombies
{
    public class Pistol : Weapon
    {
        public override void Fire()
        {
            if (MainCharacter.Current.Target == null)
            {
                return;
            }
            MainCharacter.Current.Realm.Add(new Bullet(MainCharacter.Current.Target.Bounds.Location));
        }
    }
}
