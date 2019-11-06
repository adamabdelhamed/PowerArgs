using PowerArgs.Cli.Physics;
using System;
using System.Linq;
namespace PowerArgs.Games
{


    public class Character : SpacialElement, IObservableObject
    {
        public Event<float> OnMove { get; private set; } = new Event<float>();


        public bool IsVisible { get => observable.Get<bool>(); set => observable.Set(value); } 
        public MultiPlayerClient MultiPlayerClient { get; set; }
        public char? Symbol { get; set; }
        public Inventory Inventory { get => observable.Get<Inventory>(); set => observable.Set(value); }

        public float MaxMovementSpeed { get; set; } = 25;
        public float CurrentSpeedPercentage { get; set; } = .8f;
        public float PlayerMovementSpeed => MaxMovementSpeed * CurrentSpeedPercentage;


        protected ObservableObject observable;
        public bool SuppressEqualChanges { get; set; }
        public object GetPrevious(string name) => observable.GetPrevious<object>(name);
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);
        public SpacialElement Target { get; set; }


        public float TargetAngle => Target == null ? Speed.Angle : this.CalculateAngleTo(Target.Center());
        public SpeedTracker Speed { get; set; }


        public AutoTargetingFunction Targeting { get; protected set; }
        public Cursor FreeAimCursor { get; set; }
        public AimMode AimMode
        {
            get
            {
                return FreeAimCursor != null ? AimMode.Manual : AimMode.Auto;
            }
        }


        public Character()
        {
            observable = new ObservableObject(this);
            IsVisible = true;
            this.SubscribeForLifetime(nameof(Inventory), () => this.Inventory.Owner = this, this.Lifetime);
            Inventory = new Inventory();
            Speed = new SpeedTracker(this) { Bounciness = 0 };
            this.ResizeTo(1, 1);
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
                Speed.SpeedX = -PlayerMovementSpeed.NormalizeQuantity(0);
                Speed.SpeedY = 0;
                RoundOff();
            }

            TrySendBounds();
            OnMove.Fire(180);
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
                Speed.SpeedX = PlayerMovementSpeed.NormalizeQuantity(0);
                Speed.SpeedY = 0;
                RoundOff();
            }

            TrySendBounds();
            OnMove.Fire(0);
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
                Speed.SpeedY = PlayerMovementSpeed.NormalizeQuantity(90);
                Speed.SpeedX = 0;
                RoundOff();
            }

            TrySendBounds();
            OnMove.Fire(90);
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
                Speed.SpeedY = -PlayerMovementSpeed.NormalizeQuantity(90);
                Speed.SpeedX = 0;
                RoundOff();
            }

            TrySendBounds();
            OnMove.Fire(270);
        }

        private void RoundOff()
        {
            var newLeft = (float)Math.Round(this.Left);
            var newTop = (float)Math.Round(this.Top);
            this.MoveTo(newLeft, newTop);
        }

        public void TrySendBounds()
        {
            MultiPlayerClient?.TrySendMessage(new BoundsMessage()
            {
                X = Left,
                Y = Top,
                SpeedX = Speed.SpeedX,
                SpeedY = Speed.SpeedY,
                ClientToUpdate = MultiPlayerClient.ClientId
            });
        }

        private void EndFreeAim()
        {
            FreeAimCursor?.Lifetime.Dispose();
            FreeAimCursor = null;
            observable.FirePropertyChanged(nameof(AimMode));
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
        
        public void InitializeTargeting()
        {
            Targeting = new AutoTargetingFunction(new AutoTargetingOptions()
            {
                Source = this.Speed,
                TargetsEval = () => SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag("enemy")),
            });
            Added.SubscribeForLifetime(() => { Time.CurrentTime.Add(Targeting); }, this.Lifetime);
            this.Lifetime.OnDisposed(Targeting.Lifetime.Dispose);

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
            }, this.Lifetime);
        }
        public void TryInteract() => SpaceTime.CurrentSpaceTime.Elements
       .Where(e => e is IInteractable)
       .Select(i => i as IInteractable)
       .Where(i => i.InteractionPoint.CalculateDistanceTo(this) <= i.MaxInteractDistance)
       .ForEach(i => i.Interact(this));

    }

}
