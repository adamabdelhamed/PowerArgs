using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class Projectile : WeaponElement
    {
        public static Event<Impact> OnAudibleImpact { get; private set; } = new Event<Impact>();

        public const float StandardWidth = 1f;
        public const float StandardHeight = 1f;

        public ConsoleString Pen { get; set; } = new ConsoleString("*", ConsoleColor.Red);
        public float Range { get; set; } = -1;

        public Velocity Velocity { get; private set; }
        public bool PlaySoundOnImpact { get; set; }

        private IRectangularF startLocation;
        private Force force;
        public Projectile(Weapon w, float speed, float angle) : base(w)
        {
            this.ResizeTo(StandardWidth, StandardHeight);
            if (w?.Holder != null)
            {
                this.MoveTo(w.Holder.CenterX()-StandardWidth/2, w.Holder.CenterY()-StandardHeight/2, w.Holder.ZIndex);
            }

            Time.CurrentTime.QueueAction("Snap bounds",() => startLocation = this.Bounds);

            this.Tags.Add(Weapon.WeaponTag);
            Velocity = new Velocity(this);
            Velocity.Governor.Rate = TimeSpan.FromSeconds(0);
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
            

            this.Governor.Rate = TimeSpan.FromSeconds(-1);
        }
 

        public static float AnticipateAngle(Character shooter, Velocity movingTarget, float projectileSpeed, float currentAngle)
        {
            var targetCurrentBounds = movingTarget.Element.EffectiveBounds();
            var targetCurrentCenter = targetCurrentBounds.Center();
            var targetCurrentDistance = Geometry.NormalizeQuantity(shooter.Center().CalculateDistanceTo(targetCurrentCenter), currentAngle, true);
            var anticipatedTimeToImpact =   Geometry.NormalizeQuantity(  targetCurrentDistance / projectileSpeed, movingTarget.Angle, true);
            var anticipatedTargetMovement = movingTarget.Speed * anticipatedTimeToImpact;

            var anticipatedTargetCenter = SpacialAwareness.MoveTowards(targetCurrentCenter, movingTarget.Angle, anticipatedTargetMovement.NormalizeQuantity(movingTarget.Angle, true));
            var anticipatedTargetBounds = RectangularF.Create(anticipatedTargetCenter.Left - targetCurrentBounds.Width / 2,
                targetCurrentCenter.Top - targetCurrentBounds.Height / 2, targetCurrentBounds.Width, targetCurrentBounds.Height);

            var ret =  shooter.Center().CalculateAngleTo(anticipatedTargetBounds.Center());
            return ret;
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
            context.DrawString((Element as Projectile).Pen, 0, 0);
        }
    }
}
