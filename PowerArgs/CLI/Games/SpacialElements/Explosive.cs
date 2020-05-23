using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class Explosive : WeaponElement
    {
        public float ExplosiveProjectileSpeed { get; set; } =  60;

        public Event<Projectile> OnProjectileAdded { get; private set; } = new Event<Projectile>();
        public static Event<Explosive> OnExplode { get; private set; } = new Event<Explosive>();

        public float AngleIncrement { get; set; } = 30;
        public float Range { get; set; } = 10;

        public Event Exploded { get; private set; } = new Event();
        public ConsoleString ProjectilePen { get; set; }
        public Explosive(Weapon w) : base(w) 
        {
            this.AngleIncrement = 5;
            Velocity.GlobalImpactOccurred.SubscribeForLifetime((impact) =>
            {
                if(impact.MovingObject == this && CausesExplosion(impact.ObstacleHit))
                {
                    (impact.MovingObject as Explosive).Explode();
                }
                else if(impact.ObstacleHit == this && CausesExplosion(impact.MovingObject))
                {
                    (impact.ObstacleHit as Explosive).Explode();
                }
            }, this.Lifetime);
        }

        private bool CausesExplosion(IRectangularF thingHit)
        {
            if(thingHit is WeaponElement || thingHit is Character)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerable<SpacialElement> Explode()
        {
            var ret = new List<SpacialElement>();
            if (Lifetime.IsExpired) return ret;

            this.Lifetime.Dispose();
            var shrapnelSet = new List<Projectile>();
            for (float angle = 0; angle < 360; angle += AngleIncrement)
            {
                var effectiveRange = Range;

                if ((angle > 200 && angle < 340) || (angle > 20 && angle < 160))
                {
                    effectiveRange = Range / 3;
                }

                var shrapnel =SpaceTime.CurrentSpaceTime.Add(new Projectile(this.Weapon,ExplosiveProjectileSpeed, angle) { Range = effectiveRange });
                shrapnel.Tags.Add(nameof(Explosive));
                shrapnel.MoveTo(this.Left, this.Top, this.ZIndex);
                OnProjectileAdded.Fire(shrapnel);
                if(ProjectilePen != null)
                {
                    shrapnel.Pen = ProjectilePen;
                }

                shrapnelSet.Add(shrapnel);
                shrapnel.Tags.Add("hot");
                ret.Add(shrapnel);
            }

            foreach(var shrapnel in shrapnelSet)
            {
                shrapnel.Velocity.HitDetectionExclusions.AddRange(shrapnelSet.Where(s => s != shrapnel));
            }

            Exploded.Fire();
            OnExplode.Fire(this);
            return ret;
        }
    }

    [SpacialElementBinding(typeof(Explosive))]
    public class ExplosiveRenderer : SpacialElementRenderer
    {
        private ConsoleString DefaultStyle => new ConsoleString(" ", backgroundColor: ConsoleColor.DarkYellow);
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(DefaultStyle, 0, 0);
    }
}
