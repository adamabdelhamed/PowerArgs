using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleGames.Shooter
{
    public enum AimMode
    {
        Auto,
        Manual
    }

    public class MainCharacter : Character, IDestructible
    {


        private static Dictionary<SpaceTime, MainCharacter> mainCharacters = new Dictionary<SpaceTime, MainCharacter>();
        public static MainCharacter Current
        {
            get
            {
                if (SpaceTime.CurrentSpaceTime == null) return null;
                else if (mainCharacters.ContainsKey(SpaceTime.CurrentSpaceTime) == false) return null;
                return mainCharacters[SpaceTime.CurrentSpaceTime];
            }
            private set
            {
                SpaceTime.AssertTimeThread();
                if (mainCharacters.ContainsKey(SpaceTime.CurrentSpaceTime))
                {
                    mainCharacters[SpaceTime.CurrentSpaceTime] = value;
                }
                else
                {
                    mainCharacters.Add(SpaceTime.CurrentSpaceTime, value);
                }
            }
        }

        public AutoTargetingFunction Targeting { get; private set; }
        public Cursor FreeAimCursor { get; set; }
        public SpacialElement Target { get; set; }


        public AimMode AimMode
        {
            get
            {
                return FreeAimCursor != null ? AimMode.Manual : AimMode.Auto;
            }
        }

        public Event Damaged { get; private set; } = new Event();
        public Event Destroyed { get; private set; } = new Event();
        public float HealthPoints { get { return observable.Get<float>(); } set { observable.Set(value); } }  

     

        public MainCharacter()
        {
            this.Inventory = new ShooterInventory();
            HealthPoints = 100;


            InitializeTargeting();

            this.Added.SubscribeForLifetime(() => Current = this, this.Lifetime.LifetimeManager);
        }

        private void InitializeTargeting()
        {
            Targeting = new AutoTargetingFunction(() => this.Bounds, t => t is Enemy);
            Added.SubscribeForLifetime(() => { Time.CurrentTime.Add(Targeting); }, this.Lifetime.LifetimeManager);
            this.Lifetime.LifetimeManager.Manage(Targeting.Lifetime.Dispose);

            Targeting.TargetChanged.SubscribeForLifetime((target) =>
            {
                if (this.Target != null && this.Target.Lifetime.IsExpired == false)
                {
                    this.Target.SizeOrPositionChanged.Fire();
                }

                this.Target = target;

                if (this.Target != null && this.Target.Lifetime.IsExpired == false)
                {
                    this.Target.SizeOrPositionChanged.Fire();
                }
            }, this.Lifetime.LifetimeManager);
        }

        public void ToggleFreeAim()
        {
            var cursor = FreeAimCursor;
            if (cursor == null)
            {
                FreeAimCursor = new Cursor();
                FreeAimCursor.MoveTo(this.Left, this.Top);

                if (Target != null)
                {
                    FreeAimCursor.MoveTo(Target.Left, Target.Top);
                }
                SpaceTime.CurrentSpaceTime.Add(FreeAimCursor);
                Speed.SpeedX = 0;
                Speed.SpeedY = 0;
                observable.FirePropertyChanged(nameof(AimMode));
            }
            else
            {
                EndFreeAim();
            }
        }

   


        public void MoveLeft()
        {
            if (FreeAimCursor != null)
            {
                FreeAimCursor.MoveBy(-1, 0);
                return;
            }

            if (Speed.SpeedX < 0 && Math.Abs(Speed.SpeedX) > Math.Abs(Speed.SpeedY))
            {
                Speed.SpeedX = 0;
                Speed.SpeedY = 0;
            }
            else
            {
                Speed.SpeedX = -12;
                Speed.SpeedY = 0;
            }
        }

        public void MoveRight()
        {
            if (FreeAimCursor != null)
            {
                FreeAimCursor.MoveBy(1, 0);
                return;
            }

            if (Speed.SpeedX > Math.Abs(Speed.SpeedY))
            {
                Speed.SpeedX = 0;
                Speed.SpeedY = 0;
            }
            else
            {
                Speed.SpeedX = 12;
                Speed.SpeedY = 0;
            }
        }

        public void MoveDown()
        {
            if (FreeAimCursor != null)
            {
                FreeAimCursor.MoveBy(0, 1);
                return;
            }

            if (Speed.SpeedY > Math.Abs(Speed.SpeedX))
            {
                Speed.SpeedY = 0;
                Speed.SpeedX = 0;
            }
            else
            {
                Speed.SpeedY = 7;
                Speed.SpeedX = 0;
            }
        }

        public void MoveUp()
        {
            if (FreeAimCursor != null)
            {
                FreeAimCursor.MoveBy(0, -1);
                return;
            }

            if (Speed.SpeedY < 0 && Math.Abs(Speed.SpeedY) > Math.Abs(Speed.SpeedX))
            {
                Speed.SpeedY = 0;
                Speed.SpeedX = 0;
            }
            else
            {
                Speed.SpeedY = -7;
                Speed.SpeedX = 0;
            }
        }

        private void EndFreeAim()
        {
            FreeAimCursor?.Lifetime.Dispose();
            FreeAimCursor = null;
            observable.FirePropertyChanged(nameof(AimMode));
        }


    }

    [SpacialElementBinding(typeof(MainCharacter))]
    public class MainCharacterRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new PowerArgs.ConsoleCharacter('X', ConsoleColor.Magenta);
            context.DrawPoint(0, 0);
        }
    }
}
