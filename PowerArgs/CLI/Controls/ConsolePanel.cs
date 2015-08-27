using System;

namespace PowerArgs.Cli
{
    public class ConsolePanel : ConsoleControl
    {
        public ObservableCollection<ConsoleControl> Controls { get; private set; }

        public ConsolePanel()
        {
            Controls = new ObservableCollection<ConsoleControl>();

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
            foreach (var control in Controls)
            {
                Rectangle scope = context.GetScope();
                try
                {
                    context.Rescope(control.X, control.Y, control.Width, control.Height);
                    control.Paint(context);
                }
                finally
                {
                    context.Scope(scope);
                }
            }
        }
    }
}
