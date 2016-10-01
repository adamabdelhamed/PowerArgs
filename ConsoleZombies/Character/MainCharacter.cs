using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class MainCharacter : PowerArgs.Cli.Physics.Thing
    {
        [ThreadStatic]
        private static MainCharacter _instance;

        public bool IsInLevelBuilder { get; set; }

        public static MainCharacter Current
        {
            get
            {
                return _instance;
            }
            private set
            {
                if(_instance != null)
                {
                    throw new InvalidOperationException("There is already a main character in the game");
                }
                _instance = value;
            }
        }

        public SpeedTracker Speed { get; private set; }

        public Targeting Targeting { get; private set; }

        public Thing Target { get; set; }

        public Inventory Inventory { get; private set; } = new Inventory();

        public MainCharacter()
        {
            Speed = new SpeedTracker(this);
            Targeting = new Targeting(this);
            Speed.Bounciness = 0;
            Speed.HitDetectionTypes.Add(typeof(Wall));
            Added.SubscribeForLifetime(OnAdded, this.LifetimeManager);
            Removed.SubscribeForLifetime(OnRemoved, this.LifetimeManager);
       }

        public void OnAdded()
        {
            Current = this;
        }

        private void OnRemoved()
        {
            Current = null;
        }

        public override void Behave(Realm r)
        {
        }
    }

    [ThingBinding(typeof(MainCharacter))]
    public class MainCharacterRenderer : ThingRenderer
    {
        public MainCharacter MainCharacter
        {
            get
            {
                return Thing as MainCharacter;
            }
        }

        public MainCharacterRenderer()
        {
            this.TransparentBackground = true;
            this.AddedToVisualTree.SubscribeForLifetime(Added, this.LifetimeManager);
            ZIndex = 2000;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            context.Pen = new PowerArgs.ConsoleCharacter('X', ConsoleColor.Green, ConsoleColor.DarkGray);
            context.FillRect(0, 0,Width,Height);
        }

        private void Added()
        {
            if (MainCharacter.IsInLevelBuilder == false)
            {
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.D, null, FireGun, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.F, null, FireGun, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.R, null, DropRemoteMine, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.T, null, DropTimedMine, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Spacebar, null, ToggleSlowMo, this.LifetimeManager);


                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.RightArrow, null, MoveRight, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.LeftArrow, null, MoveLeft, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, MoveUp, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, MoveDown, this.LifetimeManager);
            }
        }

        private void ToggleSlowMo(ConsoleKeyInfo key)
        {
            RenderLoop.QueueAction(() =>
            {
                RenderLoop.SpeedFactor = RenderLoop.SpeedFactor == 1 ? .15f : 1f;
            });
        }
        private void MoveDown(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() => 
            {
                if(MainCharacter.Speed.SpeedY > Math.Abs(MainCharacter.Speed.SpeedX))
                {
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.Speed.SpeedX = 0;
                }
                else
                {
                    MainCharacter.Speed.SpeedY = 7;
                    MainCharacter.Speed.SpeedX = 0;
                }
            });
        }

        private void MoveUp(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.Speed.SpeedY < 0 && Math.Abs(MainCharacter.Speed.SpeedY) > Math.Abs(MainCharacter.Speed.SpeedX))
                {
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.Speed.SpeedX = 0;
                }
                else
                {
                    MainCharacter.Speed.SpeedY = -7;
                    MainCharacter.Speed.SpeedX = 0;
                }
            });
        }

        private void MoveLeft(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.Speed.SpeedX < 0 && Math.Abs(MainCharacter.Speed.SpeedX) > Math.Abs(MainCharacter.Speed.SpeedY))
                {
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.Speed.SpeedY = 0;
                }
                else
                {
                    MainCharacter.Speed.SpeedX = -7;
                    MainCharacter.Speed.SpeedY = 0;
                }
            });
        }

        private void MoveRight(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.Speed.SpeedX > Math.Abs(MainCharacter.Speed.SpeedY))
                {
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.Speed.SpeedY = 0;
                }
                else
                {
                    MainCharacter.Speed.SpeedX = 7;
                    MainCharacter.Speed.SpeedY = 0;
                }
            });
        }
        

        private void FireGun(ConsoleKeyInfo spacebar)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.Gun.Fire();
            });
        }

        private void DropRemoteMine(ConsoleKeyInfo key)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.RemoteMineDropper.Fire();
            });
        }

        private void DropTimedMine(ConsoleKeyInfo key)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.TimedMineDropper.Fire();
            });
        }
    }
}
