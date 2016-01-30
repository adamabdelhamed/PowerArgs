using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    internal class ProgressOperationControl : ConsolePanel
    {
        public ProgressOperation Operation { get; private set; }

        private StackPanel messageAndOperationsPanel;

        private Label messageLabel;
        private StackPanel actionPanel;
        private Label timeLabel;
        private Spinner spinner;

        public ProgressOperationControl(ProgressOperation operation)
        {
            this.Operation = operation;
            this.Height = 2;
            messageAndOperationsPanel = Add(new StackPanel() { Orientation = Orientation.Vertical }).Fill();

            messageLabel = messageAndOperationsPanel.Add(new Label() { Mode = LabelRenderMode.ManualSizing }).FillHoriontally();
            messageLabel.CanFocus = true;
            operation.SubscribeAndSyncNow(nameof(ProgressOperation.Message), () => 
            {
                messageLabel.Text = operation.Message;
            });

            actionPanel = messageAndOperationsPanel.Add(new StackPanel() { Orientation = Orientation.Horizontal, Height = 1, Margin = 2 }).FillHoriontally(messageAndOperationsPanel);
            spinner = actionPanel.Add(new Spinner() { CanFocus=false});
            timeLabel = actionPanel.Add(new Label() { Mode = LabelRenderMode.SingleLineAutoSize, Text = operation.StartTime.ToFriendlyPastTimeStamp().ToConsoleString() });

            operation.SubscribeAndSyncNow(nameof(ProgressOperation.State), () => 
            {
                if(operation.State == OperationState.InProgress)
                {
                    spinner.IsSpinning = true;
                }
                else if(operation.State== OperationState.Completed)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Green);
                }
                else if (operation.State == OperationState.Failed)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Red);
                }
                else if (operation.State == OperationState.CompletedWithWarnings)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.DarkYellow);
                }
                else if (operation.State == OperationState.Queued)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Gray);
                }
                else if (operation.State == OperationState.NotSet)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter('?', backgroundColor: ConsoleColor.Gray);
                }
            });

            spinner.IsSpinning = operation.State == OperationState.InProgress;

            foreach (var action in operation.Actions)
            {
                BindActionToActionPanel(action);
            }

            operation.Actions.Added += Actions_Added;
            operation.Actions.Removed += Actions_Removed;
        }

  

        private void Actions_Added(ProgressOperationAction action)
        {
            BindActionToActionPanel(action);
        }

        private void Actions_Removed(ProgressOperationAction action)
        {
            UnbindActionToActionPanel(action);
        }



        private void BindActionToActionPanel(ProgressOperationAction action)
        {
            var button = actionPanel.Add(new Button() { Text = action.DisplayName, Tag = action });
            button.Activated += action.Action;
        }

        private void UnbindActionToActionPanel(ProgressOperationAction action)
        {
            var toRemove = actionPanel.Controls.Where(c => c.Tag == action).SingleOrDefault();
            if (toRemove == null)
            {
                throw new InvalidOperationException("No action to remove");
            }

            actionPanel.Controls.Remove(toRemove);
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
        }
    }
}
