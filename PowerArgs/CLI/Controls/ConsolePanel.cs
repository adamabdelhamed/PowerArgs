using System;

namespace PowerArgs.Cli
{
    public class ConsolePanel : ConsoleControl
    {
        public ObservableCollection<ConsoleControl> Controls { get; private set; }

        public ConsolePanel()
        {
            Controls = new ObservableCollection<ConsoleControl>();
            this.CanFocus = false;
            Action<ConsoleControl> addPropagator = (c) => { Controls.FireAdded(c); };
            Action<ConsoleControl> removePropagator = (c) => { Controls.FireRemoved(c); };

            Controls.Added += (c) =>
            {
                // only hook up propogators for direct descendents
                if(Controls.Contains(c) == false)
                {
                    return;
                }

                c.Parent = this;
                if (c is ConsolePanel)
                {
                    (c as ConsolePanel).Controls.Added += addPropagator;
                    (c as ConsolePanel).Controls.Removed += removePropagator;
                }
            };

            Controls.Removed += (c) =>
            {
                if (c is ConsolePanel)
                {
                    (c as ConsolePanel).Controls.Added -= addPropagator;
                    (c as ConsolePanel).Controls.Removed -= removePropagator;
                }
                c.Parent = null;
            };
        }

        public T Add<T>(T c) where T : ConsoleControl
        {
            Controls.Add(c);
            return c;
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
