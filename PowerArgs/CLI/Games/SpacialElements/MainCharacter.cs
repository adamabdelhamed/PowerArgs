using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using PowerArgs;

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
        void Interact(Character character);
    }

    public class MainCharacter : Character
    {
        public static Event<Weapon> OnEquipWeapon { get; private set; } = new Event<Weapon>();


        public ConsoleColor Color { get; set; } = ConsoleColor.Magenta;

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

        private static int NextId = 100;

        public MainCharacter()
        {
            this.Id = nameof(MainCharacter) + ": " + NextId++;
            this.Tags.Add(nameof(MainCharacter));
            this.MoveTo(0, 0);
            this.Added.SubscribeForLifetime(() =>
            {
                Current = this;
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
                TargetsEval = () => SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag("enemy")),
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
            if (Character.IsVisible == false) return;
            char c;

            var angle = Character.Velocity.Angle;

            c = Geometry.GetArrowPointedAt(angle);

            context.Pen = new ConsoleCharacter(c, Character.Color);
            context.FillRect(0, 0, Width, Height);
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
