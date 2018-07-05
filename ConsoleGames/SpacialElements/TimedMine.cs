using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace ConsoleGames 
{
    public class TimedMine : Explosive
    {
        private TimeSpan timeToDetinate;
        public double SecondsRemaining { get; private set; }

        private bool startedTimer = false;

        public bool Silent { get; set; }

        public TimedMine(TimeSpan timeToDetinate)
        {
            this.timeToDetinate = timeToDetinate;
            this.Governor.Rate = TimeSpan.Zero;
        }

        public override void Initialize()
        {
            base.Initialize();
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
                        Sound.Play("tick");
                        var d = SpaceTime.CurrentSpaceTime.Application.SetInterval(() => Sound.Play("tick"), TimeSpan.FromSeconds(1));
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
    public class TimedMineRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter(' ', ConsoleColor.Black, backgroundColor: ConsoleColor.DarkYellow);

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(Math.Ceiling((Element as TimedMine).SecondsRemaining).ToString()[0], EffectiveStyle.ForegroundColor, EffectiveStyle.BackgroundColor);
            context.FillRect(0, 0, Width, Height);
        }
    }
}
