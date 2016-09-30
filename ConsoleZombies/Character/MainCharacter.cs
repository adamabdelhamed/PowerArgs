using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class MainCharacter : Thing
    {
        [ThreadStatic]
        private static MainCharacter _instance;

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

        public Thing Target { get; set; }

        public PathElement MoveTarget { get; set; }
        public bool MoveReverse { get; set; }

        public Inventory Inventory { get; private set; } = new Inventory();

        public MainCharacter()
        {
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
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            context.Pen = new PowerArgs.ConsoleCharacter('X', ConsoleColor.Green, ConsoleColor.Gray);
            context.DrawPoint(0, 0);
        }

        private void Added()
        {
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Spacebar, null, SpacebarPressed, this.LifetimeManager);
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.RightArrow, null, RightArrowPressed, this.LifetimeManager);
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Enter, null, EnterPressed, this.LifetimeManager);
            Application.FocusManager.SubscribeForLifetime(nameof(FocusManager.FocusedControl), FocusChanged, this.LifetimeManager);
        }

        private void FocusChanged()
        {
            if(Application.FocusManager.FocusedControl is ThingRenderer == false)
            {
                RenderLoop.QueueAction(()=> { MainCharacter.Target = null; });
                return;
            }
            else
            {
                var thing = (Application.FocusManager.FocusedControl as ThingRenderer).Thing;
                if(thing is Zombie)
                {
                    RenderLoop.QueueAction(() => { MainCharacter.Target = thing; });
                }
                else
                {
                    RenderLoop.QueueAction(() => { MainCharacter.Target = null; });
                }
            }
        }

        private void SpacebarPressed(ConsoleKeyInfo spacebar)
        {
            RenderLoop.QueueAction(() =>
            {
                if (MainCharacter.Inventory.CurrentWeapon != null && MainCharacter.Target != null)
                {
                    MainCharacter.Inventory.CurrentWeapon.Fire();
                }
            });
        }

        private void EnterPressed(ConsoleKeyInfo enter)
        {
            RenderLoop.QueueAction(() =>
            {
                if(MainCharacter.MoveTarget != null)
                {
                    new PathTraveler(MainCharacter, MainCharacter.MoveTarget, MainCharacter.MoveReverse);
                }
            });
        }

        private void RightArrowPressed(ConsoleKeyInfo rightArrow)
        {
            RenderLoop.QueueAction(() =>
            {
                var path = RenderLoop.Realm.Things.Where(t => t is PathElement).Select(t => t as PathElement).First().Path;

                var startDistance = MainCharacter.Bounds.Location.CalculateDistanceTo(path.First().Bounds.Location);
                var endDistance = MainCharacter.Bounds.Location.CalculateDistanceTo(path.Last().Bounds.Location);
                if (startDistance < endDistance)
                {
                    path.Last().IsHighlighted = true;
                    path.First().IsHighlighted = false;
                    MainCharacter.MoveReverse = false;
            
                }
                else
                {
                    path.Last().IsHighlighted = false;
                    path.First().IsHighlighted = true;
                    MainCharacter.MoveReverse = true;
                }

                MainCharacter.MoveTarget = path.OrderBy(p => MainCharacter.Bounds.Location.CalculateDistanceTo(p.Bounds.Location)).First();

            });
        }
    }
}
