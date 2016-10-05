using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class MainCharacter : Thing, IDestructible
    {
        [ThreadStatic]
        private static MainCharacter _instance;

        public Event EatenByZombie { get; private set; } = new Event();

        public bool IsInLevelBuilder { get; set; }

        public static MainCharacter Current
        {
            get
            {
                return _instance;
            }
            private set
            {
                if(_instance != null && value != null)
                {
                    throw new InvalidOperationException("There is already a main character in the game");
                }
                _instance = value;
            }
        }

        public void EndFreeAim()
        {
           Realm.Remove(FreeAimCursor);
           FreeAimCursor = null;
        }

        public SpeedTracker Speed { get; private set; }

        public Targeting Targeting { get; private set; }

        public Cursor FreeAimCursor { get; set; }

        public Thing Target { get; set; }

        public Inventory Inventory { get; private set; } = new Inventory();

        public float HealthPoints { get; set; }

        public MainCharacter()
        {
            Speed = new SpeedTracker(this);
            Targeting = new Targeting(this);
            Speed.Bounciness = 0;
            Speed.HitDetectionTypes.Add(typeof(Wall));
            Speed.HitDetectionTypes.Add(typeof(Item));
            Speed.ImpactOccurred.SubscribeForLifetime(ImpactOccurred, this.LifetimeManager);

            Added.SubscribeForLifetime(OnAdded, this.LifetimeManager);
            Removed.SubscribeForLifetime(OnRemoved, this.LifetimeManager);
       }

        private void ImpactOccurred(Impact impact)
        {
            if(impact.ThingHit is Item)
            {
                var inventoryItem = (impact.ThingHit as Item).Convert();
                Realm.Remove(impact.ThingHit);
                Inventory.Add(inventoryItem);
            }
        }

        public void OnAdded()
        {
            Current = this;
        }

        private void OnRemoved()
        {
            Current = null;
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
            context.Pen = new PowerArgs.ConsoleCharacter('X', ConsoleColor.Green);
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
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.G, null, LaunchRPG, this.LifetimeManager);

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Enter, null, TryOpenOrCloseDoor, this.LifetimeManager);


                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Spacebar, null, ToggleSlowMo, this.LifetimeManager);

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.A, null, FreeAim, this.LifetimeManager);


                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.RightArrow, null, MoveRight, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.LeftArrow, null, MoveLeft, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, MoveUp, this.LifetimeManager);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, MoveDown, this.LifetimeManager);
            }
        }

        private void FreeAim(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                var cursor = MainCharacter.FreeAimCursor;
                if(cursor == null)
                {
                    MainCharacter.FreeAimCursor = new Cursor() { Bounds = MainCharacter.Bounds.Clone() };

                    if(MainCharacter.Target != null && MainCharacter.IsExpired == false)
                    {
                        MainCharacter.FreeAimCursor.Bounds.MoveTo(MainCharacter.Target.Bounds.Location);
                        MainCharacter.FreeAimCursor.RoundToNearestPixel();
                    }

                    MainCharacter.Realm.Add(MainCharacter.FreeAimCursor);
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.Speed.SpeedY = 0;
                }
                else
                {
                    MainCharacter.EndFreeAim();
                }
            });
        }

        private void TryOpenOrCloseDoor(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                var door = (Door)MainCharacter.Realm.Things
                    .Where(t => t is Door).OrderBy(d => MainCharacter.Bounds.Location.CalculateDistanceTo(d.Bounds.Location)).FirstOrDefault();

                if(door != null && MainCharacter.Bounds.Location.CalculateDistanceTo(door.Bounds.Location) <= 2.5)
                {
                    door.IsOpen = !door.IsOpen;

                    if (RealmHelpers.GetThingsITouch(MainCharacter.Realm, MainCharacter, new List<Type>() { typeof(Door) }).Contains(door))
                    {
                        if (door.IsOpen)
                        {
                            MainCharacter.Bounds.MoveTo(door.ClosedBounds.Location);
                        }
                        else
                        {
                            MainCharacter.Bounds.MoveTo(door.OpenLocation);
                        }
                    }

                    MainCharacter.Realm.Update(door);
                    MainCharacter.Realm.Update(MainCharacter);
                }
            });
        }
        private void LaunchRPG(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.RPGLauncher.TryFire();
            });
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
                if(MainCharacter.FreeAimCursor != null)
                {
                    RealmHelpers.MoveThingSafeBy(RenderLoop.Realm, MainCharacter.FreeAimCursor, 0, 1);
                    return;
                }

                if(MainCharacter.Speed.SpeedY > Math.Abs(MainCharacter.Speed.SpeedX))
                {
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.RoundToNearestPixel();

                }
                else
                {
                    MainCharacter.Speed.SpeedY = 7;
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.RoundToNearestPixel();
                }
            });
        }

        private void MoveUp(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.FreeAimCursor != null)
                {
                    RealmHelpers.MoveThingSafeBy(RenderLoop.Realm, MainCharacter.FreeAimCursor, 0, -1);
                    return;
                }

                if (MainCharacter.Speed.SpeedY < 0 && Math.Abs(MainCharacter.Speed.SpeedY) > Math.Abs(MainCharacter.Speed.SpeedX))
                {
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.RoundToNearestPixel();
                }
                else
                {
                    MainCharacter.Speed.SpeedY = -7;
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.RoundToNearestPixel();
                }
            });
        }

        private void MoveLeft(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.FreeAimCursor != null)
                {
                    RealmHelpers.MoveThingSafeBy(RenderLoop.Realm, MainCharacter.FreeAimCursor, -1, 0);
                    return;
                }

                if (MainCharacter.Speed.SpeedX < 0 && Math.Abs(MainCharacter.Speed.SpeedX) > Math.Abs(MainCharacter.Speed.SpeedY))
                {
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.RoundToNearestPixel();
                }
                else
                {
                    MainCharacter.Speed.SpeedX = -7;
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.RoundToNearestPixel();
                }
            });
        }

        private void MoveRight(ConsoleKeyInfo obj)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.FreeAimCursor != null)
                {
                    RealmHelpers.MoveThingSafeBy(RenderLoop.Realm, MainCharacter.FreeAimCursor, 1, 0);
                    return;
                }

                if (MainCharacter.Speed.SpeedX > Math.Abs(MainCharacter.Speed.SpeedY))
                {
                    MainCharacter.Speed.SpeedX = 0;
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.RoundToNearestPixel();
                }
                else
                {
                    MainCharacter.Speed.SpeedX = 7;
                    MainCharacter.Speed.SpeedY = 0;
                    MainCharacter.RoundToNearestPixel();
                }
            });
        }
        

        private void FireGun(ConsoleKeyInfo spacebar)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.Gun.TryFire();
                SoundEffects.Instance.PlaySound("pistol");
            });
        }

        private void DropRemoteMine(ConsoleKeyInfo key)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.RemoteMineDropper.TryFire();
            });
        }

        private void DropTimedMine(ConsoleKeyInfo key)
        {
            RenderLoop.QueueAction(() =>
            {
                MainCharacter.Inventory.TimedMineDropper.TryFire();
            });
        }
    }
}
