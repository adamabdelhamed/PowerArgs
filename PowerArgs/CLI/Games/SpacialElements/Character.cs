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

            if (Speed.Angle.RoundAngleToNearest(90) == 180 && Speed.Speed > 0)
            {
                Speed.Speed = 0;
                Speed.Angle = 180;
            }
            else
            {
                Speed.Angle = 180;
                Speed.Speed = PlayerMovementSpeed;
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

            if (Speed.Angle.RoundAngleToNearest(90) == 0 && Speed.Speed > 0)
            {
                Speed.Speed = 0;
                Speed.Angle = 0;
            }
            else
            {
                Speed.Angle = 0;
                Speed.Speed = PlayerMovementSpeed;
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

            if (Speed.Angle.RoundAngleToNearest(90) == 90 && Speed.Speed > 0)
            {
                Speed.Angle = 90;
                Speed.Speed = 0;
            }
            else
            {
                Speed.Angle = 90;
                Speed.Speed = PlayerMovementSpeed;
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

            if (Speed.Angle.RoundAngleToNearest(90) == 270 && Speed.Speed > 0)
            {
                Speed.Angle = 270;
                Speed.Speed = 0;
            }
            else
            {
                Speed.Angle = 270;
                Speed.Speed = PlayerMovementSpeed;
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
                Speed = Speed.Speed,
                Angle = Speed.Angle,
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
                Speed.Stop();
                observable.FirePropertyChanged(nameof(AimMode));
            }
            else
            {
                EndFreeAim();
            }
        }
        
        protected void InitializeTargeting(AutoTargetingFunction func)
        {
            Targeting = func;
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

        public void DisableTargeting()
        {
            Time.CurrentTime.Functions.WhereAs<AutoTargetingFunction>().Where(f => f.Options.Source == this.Speed).SingleOrDefault()?.Lifetime.Dispose();
        }

        public void OverrideTargeting(AutoTargetingFunction func)
        {
            if(Time.CurrentTime.Functions.WhereAs<AutoTargetingFunction>().Where(f => f.Options.Source == this.Speed).Single() != func)
            {
                throw new ArgumentException("You must disable targeting and add the new function to Time before overriding");
            }
            InitializeTargeting(func);
        }

        public void TryInteract() => SpaceTime.CurrentSpaceTime.Elements
       .Where(e => e is IInteractable)
       .Select(i => i as IInteractable)
       .Where(i => i.InteractionPoint.CalculateDistanceTo(this) <= i.MaxInteractDistance)
       .ForEach(i => i.Interact(this));

    }

}
