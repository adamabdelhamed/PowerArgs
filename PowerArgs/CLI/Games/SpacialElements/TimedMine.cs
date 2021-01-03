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

        public TimedMine(Weapon w, TimeSpan timeToDetinate) : base(w)
        {
            this.AddTag(Weapon.WeaponTag);
            this.timeToDetinate = timeToDetinate;
            this.SecondsRemaining = timeToDetinate.TotalSeconds;

            this.Added.SubscribeOnce(async () =>
            {
                while (this.Lifetime.IsExpired == false)
                {
                    Evaluate();
                    await Time.CurrentTime.YieldAsync();
                }
            });
        }

        private void Evaluate()
        {
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
                        var tickLt = Lifetime.EarliestOf(this.Lifetime, this.Exploded.CreateNextFireLifetime());
                        Time.CurrentTime.Invoke(async () =>
                        {
                            while(tickLt.IsExpired == false)
                            {
                                OnAudibleTick.Fire(this);
                                await Time.CurrentTime.DelayAsync(1000);
                            }
                        });
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
            context.FillRectUnsafe(0, 0, Width, Height);
        }
    }
}
