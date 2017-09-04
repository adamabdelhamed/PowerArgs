namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// Enum to represent the falling or rising action of a signal
    /// </summary>
    public enum SignalDirection
    {
        /// <summary>
        /// The signal value is increasing
        /// </summary>
        Rising,
        /// <summary>
        /// The signal value is decreasing
        /// </summary>
        Falling
    }

    /// <summary>
    /// Debounces a numeric signal whose value changes over time based on a threshold.
    /// </summary>
    public class DebounceableSignal
    {
        /// <summary>
        /// An event that fires when the threshold is crossed
        /// </summary>
        public Event<bool> ActiveChanged { get; private set; } = new Event<bool>();

        /// <summary>
        /// Gets or sets the active direction. If you expect the signal to remain below a given threshold
        /// then you should set this to 'Rising'. If you expect the signal to remain above a given threshold then set this to 'Falling'.
        /// </summary>
        public SignalDirection ActiveDirection { get; set; }

        /// <summary>
        /// The threshold to either remain above or below
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// The amount that the signal must surpass the threshold before moving from the active to the inactive state
        /// </summary>
        public double CoolDownAmount { get; set; }

        /// <summary>
        /// Gets the current active state of the signal
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Updates the value of the signal
        /// </summary>
        /// <param name="newValue">the new value</param>
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
