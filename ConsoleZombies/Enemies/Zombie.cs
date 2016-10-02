using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Zombie : Thing, IDestructible
    {
        public float HealthPoints { get; set; }

        public bool IsBeingTargeted { get; private set; }

        private Seeker _seeker;
        public SpeedTracker SpeedTracker { get; private set; }
        public bool IsActive
        {
            get
            {
                return _seeker != null;
            }
            set
            {
                if (value == false && _seeker == null) return;
                else if (value == false) Realm.Remove(_seeker);
                else if (_seeker != null) return;
                else
                {
                    _seeker = new Seeker(this, MainCharacter.Current, SpeedTracker, 1.25f) { IsSeeking = false };
                }
            }
        }

        public Zombie()
        {
            this.SpeedTracker = new SpeedTracker(this);
            this.SpeedTracker.HitDetectionTypes.Add(typeof(Wall));
            this.SpeedTracker.HitDetectionTypes.Add(typeof(MainCharacter));
            this.SpeedTracker.ImpactOccurred += SpeedTracker_ImpactOccurred;
            this.SpeedTracker.Bounciness = 0;
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(0, 0, 1, 1);
            this.HealthPoints = 2;
        }

        private void SpeedTracker_ImpactOccurred(float angle, PowerArgs.Cli.Physics.Rectangle bounds, Thing thingHit)
        {
            if(thingHit == MainCharacter.Current)
            {
                MainCharacter.Current.EatenByZombie.Fire();
            }
        }

        public override void InitializeThing(Realm r)
        {

        }

        public override void Behave(Realm r)
        {
            if (MainCharacter.Current == null) return;

            IsBeingTargeted = MainCharacter.Current.Target == this;

            if (IsActive == false) return;

            var routeToMainCharacter = RealmHelpers.CalculateLineOfSight(r, this, MainCharacter.Current.Bounds.Location, 1);

            if(routeToMainCharacter.Obstacles.Where(o => o is Wall).Count() == 0)
            {
                _seeker.IsSeeking = true;
            }
            else
            {
                _seeker.IsSeeking = false;
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
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if ((Thing as Zombie).IsBeingTargeted)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', GameTheme.DefaultTheme.FocusColor, ConsoleColor.DarkGray);
            }
            else
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', ConsoleColor.DarkRed);
            }
            context.FillRect(0, 0,Width,Height);
        }
    }
}
