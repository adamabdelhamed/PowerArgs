using PowerArgs.Cli;

namespace PowerArgs.Cli.Physics
{
    public enum SignalDirection
    {
        Rising,
        Falling
    }

    public class DebounceableSignal
    {
        public Event<bool> ActiveChanged { get; private set; } = new Event<bool>();
        public SignalDirection ActiveDirection { get; set; }
        public double Threshold { get; set; }
        public double CoolDownAmount { get; set; }
        public bool Active { get; private set; }

        public void Update(double newValue)
        {
            if (ActiveDirection == SignalDirection.Rising)
            {
                if (newValue > Threshold && !Active)
                {
                    Active = true;
                    ActiveChanged.Fire(Active);
                }
                else if (newValue <= (Threshold - CoolDownAmount) && Active)
                {
                    Active = false;
                    ActiveChanged.Fire(Active);
                }
            }
            else if (ActiveDirection == SignalDirection.Falling)
            {
                if (newValue < Threshold && !Active)
                {
                    Active = true;
                    ActiveChanged.Fire(Active);
                }
                else if (newValue >= (Threshold + CoolDownAmount) && Active)
                {
                    Active = false;
                    ActiveChanged.Fire(Active);
                }
            }
        }
    }
}
