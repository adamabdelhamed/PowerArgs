using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace ConsoleGames
{
    public abstract class Explosive : SpacialElement
    {
        public float HealthPointsPerShrapnel { get; set; }

        private float angleIcrement;
        private float range;

        public Explosive(float x, float y, float angleIcrement, float range) 
        {
            this.MoveTo(x, y);
            this.HealthPointsPerShrapnel = 1;
            this.angleIcrement = angleIcrement;
            this.range = range;
        }

        public void Explode()
        {
            Sound.Play("boom");
            for (float angle = 0; angle < 360; angle += angleIcrement)
            {
                var effectiveRange = range;

                if ((angle > 200 && angle < 340) || (angle > 20 && angle < 160))
                {
                    effectiveRange = range / 3;
                }

                var shrapnel = new Projectile(this.Left, this.Top, angle) { HealthPoints = HealthPointsPerShrapnel, Range = effectiveRange };
                shrapnel.Tags.Add("hot");
                SpaceTime.CurrentSpaceTime.Add(shrapnel);
            }

            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Explosive))]
    public class ExplosiveRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter(' ', backgroundColor: ConsoleColor.DarkYellow);
    }
}
