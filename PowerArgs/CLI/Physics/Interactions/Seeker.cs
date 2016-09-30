using System;

namespace PowerArgs.Cli.Physics
{
    public class Seeker : ThingInteraction
    {
        public Thing SeekTarget { get; set; }
        public float Speed { get; set; }

        public Seeker() { }

        public Seeker(Thing seeker, Thing seekTarget, float speed) : base(seeker)
        {
            this.SeekTarget = seekTarget;
            this.Speed = speed;
        }

        public override void Initialize(Realm realm)
        {
            base.Initialize(realm);
        }

        public override void Behave(Realm realm)
        {
            base.Behave(realm);

            if (CheckSeekComplete(realm)) return;
            if (LastBehavior == TimeSpan.Zero) LastBehavior = realm.ElapsedTime;
            float dt = (float)(realm.ElapsedTime.TotalSeconds - LastBehavior.TotalSeconds);
            float distance = dt * Speed;
            MyThing.Bounds.Location = RealmHelpers.MoveTowards(MyThing.Bounds.Location, SeekTarget.Bounds.Location, distance);
            RealmHelpers.MoveThingSafeBy(realm, MyThing, 0, 0);
            realm.Update(MyThing);
            CheckSeekComplete(realm);
        }

        private bool CheckSeekComplete(Realm realm)
        {
            if (MyThing.Bounds.Hits(SeekTarget.Bounds))
            {
                realm.Remove(this);
                return true;
            }
            return false;
        }
    }
}
