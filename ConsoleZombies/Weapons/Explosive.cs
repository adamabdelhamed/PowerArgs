using PowerArgs.Cli.Physics;
using System;


namespace ConsoleZombies
{
    public abstract class Explosive : Thing
    {
        public float HealthPointsPerShrapnel { get; set; }

        private float angleIcrement;
        private float range;

        public Explosive(PowerArgs.Cli.Physics.Rectangle bounds, float angleIcrement, float range) : base(bounds.Clone())
        {
            this.HealthPointsPerShrapnel = 1;
            this.angleIcrement = angleIcrement;
            this.range = range;
        }

        public void Explode()
        {
            if (this.Realm == null) return;

            SoundEffects.Instance.PlaySound("boom");
            for (float angle = 0; angle < 360; angle += angleIcrement)
            {
                Bullet shrapnel = new Bullet(this.Bounds.Location, angle) { HealthPoints=HealthPointsPerShrapnel,  Range = range };
                Realm.Add(shrapnel);
            }

            Realm.Remove(this);
        }
    }

    [ThingBinding(typeof(Explosive))]
    public class ExplosiveRenderer : ThingRenderer
    {
        public ExplosiveRenderer()
        {
                Background = ConsoleColor.DarkYellow;
        }
    }
}
