using System;
using System.Linq;

namespace PowerArgs.Cli
{
    public class NotificationButton : ConsolePanel
    {
        Button launcher;
        Spinner spinner;
        ProgressOperationsManager manager;

        public NotificationButton(ProgressOperationsManager manager)
        {
            this.manager = manager;
            
            launcher = Add(new Button());
            launcher.Shortcut = new KeyboardShortcut(ConsoleKey.N, true);
            launcher.Activated += NotificationButton_Activated;
            
            spinner = Add(new Spinner() { IsVisible = false, IsSpinning = false, CanFocus = false, X = 1, Foreground = ConsoleColor.Cyan });
            Manager_ProgressOperationsChanged();
            manager.ProgressOperationsChanged += Manager_ProgressOperationsChanged;
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            launcher.Synchronize(nameof(Bounds), () => { this.Size = launcher.Size; });
        }

        public override void OnRemovedFromVisualTree()
        {
            base.OnRemovedFromVisualTree();
        }

        private void Manager_ProgressOperationsChanged()
        {
            int numberOfOperations = manager.Operations.Count;
            int numberOfInProgressOperations = manager.Operations.Where(o => o.State == OperationState.InProgress).Count();

            if(numberOfInProgressOperations == 0)
            {
                spinner.IsSpinning = false;
                spinner.IsVisible = false;
                launcher.Text = numberOfOperations+" notifications";
            }
            else
            {
                spinner.IsVisible = true;
                spinner.IsSpinning = true;
                launcher.Text = " "+numberOfOperations+ " notifications";
            }
        }

        private void NotificationButton_Activated()
        {
            var app = Application as ConsolePageApp;
            if (app == null) throw new NotSupportedException("NotificationButton can only be used in a ConsolePageApp");
            app.PageStack.CurrentPage.ShowProgressOperationsDialog();
        }
    }
}
