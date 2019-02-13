namespace PowerArgs.Cli
{
    /// <summary>
    /// A class representing a pixel in a ConsoleBitmap
    /// </summary>
    public class ConsolePixel
    {
        /// <summary>
        /// The value of the pixel
        /// </summary>
        public ConsoleCharacter? Value { get; set; }

        /// <summary>
        /// The last value that was painted. This facilitates a double
        /// buffering strategy for better performance
        /// </summary>
        public ConsoleCharacter? LastDrawnValue { get; private set; }

        /// <summary>
        /// returns true if this pixel has changed since the last time it
        /// was drawn, false otherwise
        /// </summary>
        public bool HasChanged
        {
            get
            {
                if(Value.HasValue == false && LastDrawnValue.HasValue == false)
                {
                    return false;
                }
                else if(LastDrawnValue.HasValue ^ Value.HasValue)
                {
                    return true;
                }
                else if(LastDrawnValue.Value.Equals(Value.Value))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Marks this pixel is drawn so that it will report an unchanged
        /// state until its value changes
        /// </summary>
        public void Sync()
        {
            this.LastDrawnValue = Value;
        }

        /// <summary>
        /// Clears this pixel's last drawn value so it will report as changed
        /// the next time it is inspected
        /// </summary>
        public void Invalidate()
        {
            this.LastDrawnValue = null;
        }
    }
}
