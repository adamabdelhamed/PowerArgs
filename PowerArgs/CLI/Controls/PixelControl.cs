using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that renders a single pixel
    /// </summary>
    public class PixelControl : ConsoleControl
    {
        /// <summary>
        /// Gets or sets the character value to be displayed
        /// </summary>
        public ConsoleCharacter Value
        {
            get
            {
                var ret = Get<ConsoleCharacter>();
                return ret;
            }
            set
            {
                Set(value);
            }
        }

        /// <summary>
        /// Creates a new pixel control
        /// </summary>
        public PixelControl()
        {
            Width = 1;
            Height = 1;
            SubscribeForLifetime(nameof(Bounds), EnsureNoResize, this.LifetimeManager);
            Value = new ConsoleCharacter(' ', Foreground, Background);
        }

        private void EnsureNoResize()
        {
            if(Width != 1 || Height != 1)
            {
                throw new InvalidOperationException(nameof(PixelControl) + " must be 1 X 1");
            }
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            context.Pen = Value;
            context.DrawPoint(0,0);
        }
    }
}
