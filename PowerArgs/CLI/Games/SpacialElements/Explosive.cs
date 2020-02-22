using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class Explosive : WeaponElement
    {
        public const float ExplosiveProjectileSpeed = 60;
        [ThreadStatic]
        private static Lifetime explosiveWatcherLifetime;

        public Event<Projectile> OnProjectileAdded { get; private set; } = new Event<Projectile>();
        public static Event<Explosive> OnExplode { get; private set; } = new Event<Explosive>();

        public float AngleIncrement { get; set; } = 5;
        public float Range { get; set; } = 10;

        public Event Exploded { get; private set; } = new Event();
        public ConsoleString ProjectilePen { get; set; }
        public Explosive(Weapon w) : base(w) 
        {
            this.AngleIncrement = 5;

            if(explosiveWatcherLifetime == null)
            {
                explosiveWatcherLifetime = new Lifetime();
                Velocity.GlobalImpactOccurred.SubscribeForLifetime((impact) =>
                {
                    if(impact.MovingObject is Explosive)
                    {
                        (impact.MovingObject as Explosive).Explode();
                    }
                    else if(impact.ObstacleHit is Explosive)
                    {
                        (impact.ObstacleHit as Explosive).Explode();
                    }
                }, explosiveWatcherLifetime);
            }

            this.Lifetime.OnDisposed(()=>
            {
                if(SpaceTime.CurrentSpaceTime.Elements.WhereAs<Explosive>().None())
                {
                    explosiveWatcherLifetime.Dispose();
                    explosiveWatcherLifetime = null;
                }
            });
        }

        public void Explode()
        {
            if (Lifetime.IsExpired) return;

            var shrapnelSet = new List<Projectile>();
            for (float angle = 0; angle < 360; angle += AngleIncrement)
            {
                var effectiveRange = Range;

                if ((angle > 200 && angle < 340) || (angle > 20 && angle < 160))
                {
                    effectiveRange = Range / 3;
                }

                var shrapnel =SpaceTime.CurrentSpaceTime.Add(new Projectile(this.Weapon,ExplosiveProjectileSpeed, angle) { Range = effectiveRange });
                shrapnel.MoveTo(shrapnel.Left, shrapnel.Top, this.ZIndex);
                OnProjectileAdded.Fire(shrapnel);
                if(ProjectilePen != null)
                {
                    shrapnel.Pen = ProjectilePen;
                }

                shrapnelSet.Add(shrapnel);
                shrapnel.Tags.Add("hot");
            }

            foreach(var shrapnel in shrapnelSet)
            {
                shrapnel.Velocity.HitDetectionExclusions.AddRange(shrapnelSet.Where(s => s != shrapnel));
            }

            Exploded.Fire();
            OnExplode.Fire(this);
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
