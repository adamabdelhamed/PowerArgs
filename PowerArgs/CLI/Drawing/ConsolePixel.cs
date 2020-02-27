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
        public ConsoleCharacter? Value;

        /// <summary>
        /// The last value that was painted. This facilitates a double
        /// buffering strategy for better performance
        /// </summary>
        public ConsoleCharacter? LastDrawnValue;

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

        internal ConsolePixel()
        {

        }
    }
}
