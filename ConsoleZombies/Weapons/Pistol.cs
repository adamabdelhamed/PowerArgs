using System;

namespace ConsoleZombies
{
    public class Pistol : Weapon
    {
        public override void Fire()
        {
            var angle = MainCharacter.Current.Target != null ?
                MainCharacter.Current.Bounds.Location.CalculateAngleTo(MainCharacter.Current.Target.Bounds.Location) :
                MainCharacter.Current.Speed.Angle;

            if(MainCharacter.Current.FreeAimCursor != null)
            {
                angle = MainCharacter.Current.Bounds.Location.CalculateAngleTo(MainCharacter.Current.FreeAimCursor.Bounds.Location);

            }

            var bullet = new Bullet(MainCharacter.Current.Bounds.Location, angle) { PlaySoundOnImpact = true };
            bullet.Speed.HitDetectionTypes.Remove(typeof(MainCharacter));
            MainCharacter.Current.Realm.Add(bullet);
        }
    }
}
