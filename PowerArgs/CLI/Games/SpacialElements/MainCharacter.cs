using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    public enum AimMode
    {
        Auto,
        Manual
    }

    public interface IInteractable
    {
        float MaxInteractDistance { get;  } 
        IRectangularF InteractionPoint { get; }
        Task Interact(Character character);
    }

    public class Interactable : SpacialElement, IInteractable
    {
        public float MaxInteractDistance { get; set; }

        public IRectangularF InteractionPoint { get; set; }

        public Func<Character,Task> InteractFunc { get; set; }

        public Task Interact(Character character) => InteractFunc(character);
    }

    public class MainCharacter : Character
    {
        public static Event<Weapon> OnEquipWeapon { get; private set; } = new Event<Weapon>();


        public ConsoleColor Color { get; set; } = ConsoleColor.Magenta;

        [ThreadStatic]
        private static MainCharacter _current;
        public static MainCharacter Current
        {
            get
            {
                return _current;
            }
            private set
            {
                SpaceTime.AssertTimeThread();
                if (_current != null) throw new Exception("Already a Main character");
                _current = value;
            }
        }

        private static int NextId = 100;

        public MainCharacter()
        {
            this.Id = nameof(MainCharacter) + ": " + NextId++;
            this.AddTag(nameof(MainCharacter));
            this.MoveTo(0, 0);
            this.Added.SubscribeForLifetime(() =>
            {
                Current = this;
                this.Lifetime.OnDisposed(() =>
                {
                    if(_current == this)
                    {
                        _current = null;
                    }
                });
            }, this.Lifetime);

            this.Inventory.SubscribeForLifetime(nameof(Inventory.PrimaryWeapon), () =>
            {
                if (Inventory.PrimaryWeapon != null) OnEquipWeapon.Fire(Inventory.PrimaryWeapon);
            }, this.Lifetime);

            this.Inventory.SubscribeForLifetime(nameof(Inventory.ExplosiveWeapon), () =>
            {
                if (Inventory.PrimaryWeapon != null) OnEquipWeapon.Fire(Inventory.ExplosiveWeapon);
            }, this.Lifetime);
            InitializeTargeting(SpaceTime.CurrentSpaceTime.Add(new AutoTargetingFunction(new AutoTargetingOptions()
            {
                Source = this.Velocity,
                TargetTag = "enemy",
            })));
        }


        public void RegisterItemForPickup(SpacialElement item, Action afterPickup)
        {
            this.Velocity.ImpactOccurred.SubscribeForLifetime((i) =>
            {
                if (i.ObstacleHit == item)
                {
                    afterPickup();
                    item.Lifetime.Dispose();
                }
            }, item.Lifetime);
        }












    }

    [SpacialElementBinding(typeof(MainCharacter))]
    public class MainCharacterRenderer : SpacialElementRenderer
    {
        public MainCharacter Character => Element as MainCharacter;

        public MainCharacterRenderer()
        {
            TransparentBackground = true;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if (Character.IsVisible == false)
            {
                context.Pen = new ConsoleCharacter(' ', RGB.Black);
                context.FillRectUnsafe(0, 0, Width, Height);
                return;
            }
            char c;

            var angle = Character.Velocity.Angle;

            c = Geometry.GetArrowPointedAt(angle);

            context.Pen = Character.Pen.HasValue ? Character.Pen.Value : new ConsoleCharacter(c, Character.Color);
            context.FillRectUnsafe(0, 0, Width, Height);
        }
    }

    public class MainCharacterReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if (item.Tags.Contains("main-character") == false)
            {
                hydratedElement = null;
                return false;
            }

            hydratedElement = new MainCharacter();
            return true;
        }
    }
}
