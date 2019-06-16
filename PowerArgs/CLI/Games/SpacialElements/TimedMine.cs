using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace PowerArgs.Games 
{
    public class TimedMine : Explosive
    {
        public static Event<TimedMine> OnAudibleTick { get; private set; } = new Event<TimedMine>();

        private TimeSpan timeToDetinate;
        public double SecondsRemaining { get; private set; }

        private bool startedTimer = false;

        public bool Silent { get; set; }

        public TimedMine(TimeSpan timeToDetinate)
        {
            this.Tags.Add(Weapon.WeaponTag);
            this.timeToDetinate = timeToDetinate;
            this.Governor.Rate = TimeSpan.Zero;
            this.SecondsRemaining = timeToDetinate.TotalSeconds;
        }

        public override void Evaluate()
        {
            base.Evaluate();
           
            if (this.CalculateAge() >= timeToDetinate)
            {
                Explode();
            }
            else
            {
                SecondsRemaining = (timeToDetinate - this.CalculateAge()).TotalSeconds;
                this.SizeOrPositionChanged.Fire();
                if (startedTimer == false && SecondsRemaining <= 3)
                {
                    if (Silent == false)
                    {
                        OnAudibleTick.Fire(this);
                        var d = SpaceTime.CurrentSpaceTime.Application.SetInterval(() => OnAudibleTick.Fire(this), TimeSpan.FromSeconds(1));
                        this.Lifetime.OnDisposed(()=>
                        {
                            d.Dispose();
                        });
                        this.Exploded.SubscribeOnce(d.Dispose);
                        startedTimer = true;
                    }
                }
            }
        }
    }

    [SpacialElementBinding(typeof(TimedMine))]
    public class TimedMineRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(Math.Ceiling((Element as TimedMine).SecondsRemaining).ToString()[0], ConsoleColor.Black, ConsoleColor.DarkYellow);
            context.FillRect(0, 0, Width, Height);
        }
    }
}
