using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleZombies
{
    public class Zombie : Thing, IDestructible
    {
        public float HealthPoints { get; set; }
        public bool IsBeingTargeted { get; private set; }
        public SpeedTracker SpeedTracker { get; private set; }

        private Seeker _seeker;
        private Roamer _roamer;
    
        public bool IsActive
        {
            get
            {
                return _seeker != null;
            }
            set
            {
                if (value == false && _seeker == null)
                {
                    return;
                }
                else if (value == false)
                {
                    Scene.Remove(_seeker);
                    Scene.Remove(_roamer);
                }
                else if (_seeker != null)
                {
                    return;
                }
                else
                {
                    _seeker = new Seeker(this, MainCharacter.Current, SpeedTracker, 1.25f) { IsSeeking = false };
                    _roamer = new Roamer(this, SpeedTracker, .2f) { IsRoaming = false };
                    _roamer.Governor.Rate = TimeSpan.FromSeconds(2);
                }
            }
        }

        public Zombie()
        {
            this.SpeedTracker = new SpeedTracker(this);
            this.SpeedTracker.HitDetectionTypes.Add(typeof(Wall));
            this.SpeedTracker.HitDetectionTypes.Add(typeof(Door));
            this.SpeedTracker.HitDetectionTypes.Add(typeof(MainCharacter));
            this.SpeedTracker.ImpactOccurred.SubscribeForLifetime(ImpactOccurred, this.LifetimeManager);
            this.SpeedTracker.Bounciness = 0;
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(0, 0, 1, 1);
            this.HealthPoints = 2;
        }

        private void ImpactOccurred(Impact impact)
        {
            if(MainCharacter.Current != null && impact.ThingHit == MainCharacter.Current)
            {
                MainCharacter.Current.EatenByZombie.Fire();
            }
        }

        public override void Behave(Scene r)
        {
            if (MainCharacter.Current == null) return;
            if (IsActive == false) return;

            IsBeingTargeted = MainCharacter.Current.Target == this;

            var routeToMainCharacter = SceneHelpers.CalculateLineOfSight(r,this.Bounds, MainCharacter.Current.Bounds.Location, 1);

            if(routeToMainCharacter.Obstacles.Where(o => o is Wall).Count() == 0 && this.Bounds.Location.CalculateDistanceTo(MainCharacter.Current.Bounds.Location) < 6)
            {
                if (_seeker.IsSeeking == false)
                {
                    SoundEffects.Instance.PlaySound("zombiealert");
                    _seeker.IsSeeking = true;
                    _roamer.IsRoaming = false;
                }
            }
            else
            {
                _seeker.IsSeeking = false;
                _roamer.IsRoaming = true;
            }
        }
    }

    [ThingBinding(typeof(Zombie))]
    public class ZombieRenderer : ThingRenderer
    {
        public bool IsHighlighted { get; private set; }

        public ZombieRenderer()
        {
            this.TransparentBackground = true;
            CanFocus = false;
            ZIndex = 10;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if ((Thing as Zombie).IsBeingTargeted)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', (Thing as Zombie).HealthPoints < 2 ? ConsoleColor.Gray : ConsoleColor.DarkRed, ConsoleColor.Cyan);
            }
            else
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', (Thing as Zombie).HealthPoints < 2 ? ConsoleColor.Gray : ConsoleColor.DarkRed);
            }
            context.FillRect(0, 0,Width,Height);
        }
    }
}
