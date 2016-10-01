using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ConsoleZombies
{
    public class Zombie : Thing
    {
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
                else _seeker = new Seeker(this, MainCharacter.Current, SpeedTracker,2);
            }
        }

        public Zombie()
        {
            this.SpeedTracker = new SpeedTracker(this);
            this.SpeedTracker.HitDetectionTypes.Add(typeof(Wall));
            this.SpeedTracker.Bounciness = 0;
            this.Bounds = new PowerArgs.Cli.Physics.Rectangle(0, 0, 1, 1);
        }

        public override void InitializeThing(Realm r)
        {

        }
    }

    [ThingBinding(typeof(Zombie))]
    public class ZombieRenderer : ThingRenderer
    {
        public ZombieRenderer()
        {
            this.TransparentBackground = true;
            CanFocus = true;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if (HasFocus)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', GameTheme.DefaultTheme.FocusColor, ConsoleColor.DarkGray);
            }
            else
            {
                context.Pen = new PowerArgs.ConsoleCharacter('Z', ConsoleColor.DarkRed, ConsoleColor.DarkGray);
            }
            context.FillRect(0, 0,Width,Height);
        }
    }
}
