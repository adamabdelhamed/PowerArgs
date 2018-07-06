using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using PowerArgs;

namespace ConsoleGames
{
    public enum AimMode
    {
        Auto,
        Manual
    }

    public interface IInteractable
    {
        float MaxInteractDistance { get;  } 
        IRectangular InteractionPoint { get; }
        void Interact(MainCharacter character);
    }

    public class MainCharacter : Character
    {
        public float PlayerMovementSpeed { get; set; } = 17;

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
        public AimMode AimMode
        {
            get
            {
                return FreeAimCursor != null ? AimMode.Manual : AimMode.Auto;
            }
        }


     

        public MainCharacter()
        {
            HealthPoints = 100;
            InitializeTargeting();
            this.MoveTo(0, 0, int.MaxValue-2);
            this.Added.SubscribeForLifetime(() =>
            {
                Current = this;
            }, this.Lifetime);
        }

        private void InitializeTargeting()
        {
            Targeting = new AutoTargetingFunction(() => this.Bounds, t => t is Enemy);
            Added.SubscribeForLifetime(() => { Time.CurrentTime.Add(Targeting); }, this.Lifetime);
            this.Lifetime.OnDisposed(Targeting.Lifetime.Dispose);

            Targeting.TargetChanged.SubscribeForLifetime((target) =>
            {
                if (this.Target != null && this.Target.Lifetime.IsExpired == false)
                {
                    this.Target.SizeOrPositionChanged.Fire();
                }

                this.Target = target as Character;

                if (this.Target != null && this.Target.Lifetime.IsExpired == false)
                {
                    this.Target.SizeOrPositionChanged.Fire();
                }
            }, this.Lifetime);
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

        public void TryInteract() => SpaceTime.CurrentSpaceTime.Elements
            .Where(e => e is IInteractable)
            .Select(i => i as IInteractable)
            .Where(i => i.InteractionPoint.CalculateDistanceTo(this) <= i.MaxInteractDistance)
            .ForEach(i => i.Interact(this));
        

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
                Speed.SpeedX = SpaceExtensions.NormalizeQuantity(-PlayerMovementSpeed, 0);
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
                Speed.SpeedX = SpaceExtensions.NormalizeQuantity(PlayerMovementSpeed, 0); ;
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
                Speed.SpeedY = SpaceExtensions.NormalizeQuantity(PlayerMovementSpeed, 90);
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
                Speed.SpeedY = -SpaceExtensions.NormalizeQuantity(PlayerMovementSpeed, 90);
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
    public class MainCharacterRenderer : ThemeAwareSpacialElementRenderer
    {
        public ConsoleCharacter Style { get; set; } = new ConsoleCharacter('X', ConsoleColor.Magenta);

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = Style;
            context.FillRect(0, 0, Width, Height);
        }
    }

    public class MainCharacterReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
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
