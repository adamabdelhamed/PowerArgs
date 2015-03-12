using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class ConsolePanel : ConsoleControl
    {
        public ControlCollection Controls { get; private set; }

        int focusIndex;

        public ConsolePanel()
        {
            Controls = new ControlCollection();
            Background = new ConsoleCharacter { Value = ' ', BackgroundColor = Console.BackgroundColor, ForegroundColor = Console.ForegroundColor };

            Action<ConsoleControl> addPropagator = (c) => { Controls.FireAdded(c); };
            Action<ConsoleControl> removePropagator = (c) => { Controls.FireRemoved(c); };

            Controls.Added += (c) =>
            {
                c.Application = this.Application;
                c.OnAdd(this);
                if (c is ConsolePanel)
                {
                    (c as ConsolePanel).Controls.Added += addPropagator;
                    (c as ConsolePanel).Controls.Removed += removePropagator;
                }
            };

            Controls.Removed += (c) =>
            {
                c.OnRemove(this);
                c.Application = null;
                if (c is ConsolePanel)
                {
                    (c as ConsolePanel).Controls.Added -= addPropagator;
                    (c as ConsolePanel).Controls.Removed -= removePropagator;
                }
            };
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            context.Pen =  Background;
            context.FillRect(0, 0, Width, Height);
            foreach (var control in Controls)
            {
                Rectangle scope = context.GetScope();
                try
                {
                    context.Rescope(control.X, control.Y, control.Width, control.Height);
                    context.Pen = control.Foreground;
                    control.OnPaint(context);
                }
                finally
                {
                    context.Scope(scope);
                    context.Pen = this.Foreground;
                }
            }
        }
    }
}
