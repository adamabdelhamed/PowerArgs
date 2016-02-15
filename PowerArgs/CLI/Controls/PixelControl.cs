using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class PixelControl : ConsoleControl
    {
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

        PropertyChangedSubscription noResizeSubscription;

        public PixelControl()
        {
            Width = 1;
            Height = 1;
            noResizeSubscription = SubscribeUnmanaged(nameof(Bounds), EnsureNoResize);
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
