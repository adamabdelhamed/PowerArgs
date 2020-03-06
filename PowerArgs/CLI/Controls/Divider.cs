namespace PowerArgs.Cli
{

    /// <summary>
    /// A control that can be used to divide panels
    /// </summary>
    public class Divider : ConsoleControl
    {
        public Orientation Orientation { get; set; }

        /// <summary>
        /// Creates a divider
        /// </summary>
        public Divider()
        {
            CanFocus = false;
        }

        /// <summary>
        /// Paints the divider
        /// </summary>
        /// <param name="context">the bitmap target</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            var c = Orientation == Orientation.Vertical ? '|' : '-';
            context.FillRect(new ConsoleCharacter(c, Foreground, Background), 0, 0, Width, Height);
        }
    }
}
