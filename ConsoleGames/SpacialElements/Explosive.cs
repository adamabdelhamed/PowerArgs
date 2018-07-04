using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace ConsoleGames
{
    public class Explosive : SpacialElement
    {
        public float HealthPointsPerShrapnel { get; set; } = 5;
        public float AngleIncrement { get; set; } = 5;
        public float Range { get; set; } = 5;

        public Event Exploded { get; private set; } = new Event();

        public Explosive() 
        {
            this.AngleIncrement = 5;
            this.Range = 5;
        }

        public void Explode()
        {
            Sound.Play("boom");
            for (float angle = 0; angle < 360; angle += AngleIncrement)
            {
                var effectiveRange = Range;

                if ((angle > 200 && angle < 340) || (angle > 20 && angle < 160))
                {
                    effectiveRange = Range / 3;
                }

                var shrapnel = new Projectile(this.Left, this.Top, angle) { HealthPoints = HealthPointsPerShrapnel, Range = effectiveRange };
                shrapnel.Tags.Add("hot");
                SpaceTime.CurrentSpaceTime.Add(shrapnel);
            }

            Exploded.Fire();
            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Explosive))]
    public class ExplosiveRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter(' ', backgroundColor: ConsoleColor.DarkYellow);
    }
}
