using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ProgressOperationManagerControl : ConsolePanel
    {
        ScrollablePanel scrollablePanel;
        StackPanel operationsStackPanel;

        ProgressOperationsManager manager;
        Label noNotificationsLabel;
        public ProgressOperationManagerControl(ProgressOperationsManager manager)
        {
            this.manager = manager;
            this.scrollablePanel = Add(new ScrollablePanel()).Fill();
            operationsStackPanel = scrollablePanel.ScrollableContent.Add(new StackPanel() { Orientation = Orientation.Vertical, AutoSize=true }).FillHoriontally();
            noNotificationsLabel = Add(new Label() { Text = "No notifications".ToConsoleString(), X=1, Y=1 });
            
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            manager.Operations.Synchronize(Operations_Added, Operations_Removed, Operations_Changed);
        }

        private void Operations_Changed()
        {
            noNotificationsLabel.IsVisible = manager.Operations.Count == 0;
        }


        private void Operations_Added(ProgressOperation operation)
        {
            operationsStackPanel.Controls.Insert(0, new ProgressOperationControl(manager, operation).FillHoriontally(operationsStackPanel));
        }

        private void Operations_Removed(ProgressOperation operation)
        {
            var toRemove = operationsStackPanel.Controls.Where(c => c.Tag == operation).Single();
            operationsStackPanel.Controls.Remove(toRemove);
        }
    }
}
