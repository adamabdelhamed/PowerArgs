using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class Explosive : SpacialElement
    {
        public float AngleIncrement { get; set; } = 5;
        public float Range { get; set; } = 6;

        public Event Exploded { get; private set; } = new Event();
        public ConsoleString ProjectilePen { get; set; }
        public Explosive() 
        {
            this.AngleIncrement = 5;
            this.Range = 5;
        }

        public void Explode()
        {
            Sound.Play("boom");
            var shrapnelSet = new List<Projectile>();
            for (float angle = 0; angle < 360; angle += AngleIncrement)
            {
                var effectiveRange = Range;

                if ((angle > 200 && angle < 340) || (angle > 20 && angle < 160))
                {
                    effectiveRange = Range / 3;
                }

                var shrapnel = new Projectile(this.Left, this.Top, angle) { Range = effectiveRange };
                shrapnel.MoveTo(shrapnel.Left, shrapnel.Top, this.ZIndex);
                if(ProjectilePen != null)
                {
                    shrapnel.Pen = ProjectilePen;
                }

                shrapnelSet.Add(shrapnel);
                shrapnel.Tags.Add("hot");
                SpaceTime.CurrentSpaceTime.Add(shrapnel);
            }

            foreach(var shrapnel in shrapnelSet)
            {
                shrapnel.Speed.HitDetectionExclusions.AddRange(shrapnelSet.Where(s => s != shrapnel));
            }

            Exploded.Fire();
            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Explosive))]
    public class ExplosiveRenderer : SpacialElementRenderer
    {
        private ConsoleString DefaultStyle => new ConsoleString(" ", backgroundColor: ConsoleColor.DarkYellow);
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(DefaultStyle, 0, 0);
    }
}
