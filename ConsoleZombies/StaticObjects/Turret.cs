using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class Turret : Thing, IInteractable
    {
        public int AmmoAmount { get; set; }

        public int AmmoPerBurst { get; set; }

        private Targeting targeting;

        private Thing currentTarget;

        public bool IsFiring { get; set; }

        public bool HasTarget
        {
            get
            {
                return currentTarget != null;
            }
        }

        public Turret()
        {
            Added.SubscribeForLifetime(() => { Scene.Add(targeting = new Targeting(() => this.Bounds, Filter)); targeting.TargetChanged.SubscribeForLifetime(TargetChanged, this.LifetimeManager); }, this.LifetimeManager);
            Removed.SubscribeForLifetime(() => { Scene.Remove(targeting); }, this.LifetimeManager);
            Governor.Rate = TimeSpan.FromSeconds(.3);
            AmmoPerBurst = 2;
        }

        public override void Behave(Scene r)
        {
            if (IsFiring == false) return;
            if (currentTarget != null)
            {
                var delay = .1;
                for (int i = 0; i < AmmoPerBurst; i++)
                {
                    if (AmmoAmount == 0) break;
                    var angle = this.Bounds.Location.CalculateAngleTo(currentTarget.Bounds.Location);
                    this.LifetimeManager.Manage(Scene.SetTimeout(() =>
                    {
                        SoundEffects.Instance.PlaySound("pistol");
                        Scene.Add(new Bullet(this.Bounds.Location, angle));

                    }, TimeSpan.FromSeconds(delay)));
                    delay += .1;
                    AmmoAmount--;
                }
            }
        }

        private void TargetChanged(Thing newTarget)
        {
            if (this.currentTarget != null && this.currentTarget.IsExpired == false)
            {
                Scene.Update(this.currentTarget);
            }

            if (IsFiring == false)
            {
                this.currentTarget = null;
                return;
            }
            else
            {
                this.currentTarget = newTarget;
            }

            if (this.currentTarget != null && this.currentTarget.IsExpired == false)
            {
                Scene.Update(this.currentTarget);
            }

            Scene.Update(this);
        }

        private bool Filter(Thing thing)
        {
            return thing is Zombie;
        }

        public void Interact(MainCharacter character)
        {
            IsFiring = !IsFiring;
        }
    }

    [ThingBinding(typeof(Turret))]
    public class TurretRenderer : ThingRenderer
    {
        public TurretRenderer()
        {
            Background = ConsoleColor.Black;
        }

        public override void OnRender()
        {

            if((Thing as Turret).AmmoAmount > 20)
            {
                Foreground = ConsoleColor.DarkGreen;
            }
            else if ((Thing as Turret).AmmoAmount > 1)
            {
                Foreground = ConsoleColor.Yellow;
            }
            else
            {
                Foreground = ConsoleColor.Red;
            }

            if((Thing as Turret).IsFiring && (Thing as Turret).AmmoAmount > 0)
            {
                Background = ConsoleColor.DarkGray;
            }
            else
            {
                Background = ConsoleColor.Black;
            }
        }
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new PowerArgs.ConsoleCharacter('T', Foreground, Background);
            context.FillRect(0, 0, Width, Height);
        }
    }
}
