using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    public class ConsolePanel : ConsoleControl
    {
        public ObservableCollection<ConsoleControl> Controls { get; private set; }

        public IReadOnlyCollection<ConsoleControl> Descendents
        {
            get
            {
                List<ConsoleControl> descendends = new List<ConsoleControl>();
                VisitControlTree((d) =>
                {
                    descendends.Add(d);
                    return false;
                });

                return descendends.AsReadOnly();
            }
        }

        public ConsolePanel()
        {
            Controls = new ObservableCollection<ConsoleControl>();
            SynchronizeForLifetime(nameof(Id), () => { Controls.Id = Id; }, LifetimeManager);
            Controls.Added.SubscribeForLifetime((c) => { c.Parent = this; }, LifetimeManager);
            Controls.Removed.SubscribeForLifetime((c) => { c.Parent = null; }, LifetimeManager);
            this.CanFocus = false;
        }

        public T Add<T>(T c) where T : ConsoleControl
        {
            Controls.Add(c);
            return c;
        }

        public bool VisitControlTree(Func<ConsoleControl, bool> visitAction, ConsolePanel root = null)
        {
            bool shortCircuit = false;
            root = root ?? this;
            
            foreach(var child in root.Controls)
            {
                shortCircuit = visitAction(child);
                if (shortCircuit) return true;

                if(child is ConsolePanel)
                {
                    shortCircuit = VisitControlTree(visitAction, child as ConsolePanel);
                    if (shortCircuit) return true;
                }
            }

            return false;
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
