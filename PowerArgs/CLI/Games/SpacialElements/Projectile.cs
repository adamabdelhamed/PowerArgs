using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class Projectile : WeaponElement
    {
        public static Event<Impact> OnAudibleImpact { get; private set; } = new Event<Impact>();


        public ConsoleString Pen { get; set; } = new ConsoleString("*", ConsoleColor.Red);
        public float Range { get; set; } = -1;

        public Velocity Velocity { get; private set; }
        public bool PlaySoundOnImpact { get; set; }

        private IRectangularF startLocation;
        private Force force;
        public Projectile(Weapon w, float speed, float angle) : base(w)
        {
            this.ResizeTo(1, 1);

            if (w?.Holder != null)
            {
                this.MoveTo(w.Holder.EffectiveBounds().CenterX() - Width / 2, w.Holder.EffectiveBounds().CenterY() - Height / 2, w.Holder.ZIndex);
                var offset = this.MoveTowards(angle, 1, false);
                this.MoveTo(offset.Left, offset.Top);
            }

            Time.CurrentTime.InvokeNextCycle(() =>
            {
                if (w?.Holder != null)
                {
                    this.MoveTo(w.Holder.EffectiveBounds().CenterX() - Width / 2, w.Holder.EffectiveBounds().CenterY() - Height / 2, w.Holder.ZIndex);
                    var offset = this.MoveTowards(angle, 1, false);
                    this.MoveTo(offset.Left, offset.Top);
                }


                startLocation = this.Bounds;
            });

            this.AddTag(Weapon.WeaponTag);
            Velocity = new Velocity(this);
            Velocity.ImpactOccurred.SubscribeForLifetime(Speed_ImpactOccurred, this.Lifetime);
            this.Velocity.HitDetectionExclusionTypes.Add(typeof(Projectile));
            if (w?.Holder != null)
            {
                this.Velocity.HitDetectionExclusions.Add(w.Holder);
            }
            Velocity.Speed = speed;
            Velocity.Angle = angle;

            this.SizeOrPositionChanged.SubscribeForLifetime(() =>
            {
                if (startLocation != null && Range > 0 && this.CalculateDistanceTo(startLocation) > Range)
                {
                    this.Lifetime.Dispose();
                }
            }, this.Lifetime);
        }
 
       

        private void Speed_ImpactOccurred(Impact impact)
        {
            if (PlaySoundOnImpact)
            {
                OnAudibleImpact.Fire(impact);
            }

            DamageBroker.Instance.ReportImpact(impact);
            this.Lifetime.Dispose();
        }
    }

    [SpacialElementBinding(typeof(Projectile))]
    public class ProjectileRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            if((Element as Projectile).Pen != null && (Element as Projectile).Pen.Length > 0)
            {
                context.Pen = (Element as Projectile).Pen[0];
            }
            context.FillRect(0, 0, Width,Height);
        }
    }
}
