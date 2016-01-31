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
        private ProgressOperationsManager manager;
        public ProgressOperationControl(ProgressOperationsManager manager, ProgressOperation operation)
        {
            this.Tag = operation;
            this.Operation = operation;
            this.manager = manager;
            this.Height = 2;
            messageAndOperationsPanel = Add(new StackPanel() { Orientation = Orientation.Vertical }).Fill();

            messageLabel = messageAndOperationsPanel.Add(new Label() { Mode = LabelRenderMode.ManualSizing }).FillHoriontally();
            messageLabel.CanFocus = true;

            messageLabel.RegisterKeyHandler(ConsoleKey.Enter, () =>
            {
                var msg = operation.Message;
                if(operation.Details != null)
                {
                    msg += "\n" + operation.Details;
                }
                Dialog.ShowMessage(msg);
            });

            messageLabel.RegisterKeyHandler(ConsoleKey.Delete, () =>
            {
                var app = Application;
                manager.Operations.Remove(operation);
                app.FocusManager.TryMoveFocus();
            });

            actionPanel = messageAndOperationsPanel.Add(new StackPanel() { Orientation = Orientation.Horizontal, Height = 1, Margin = 2 }).FillHoriontally(messageAndOperationsPanel);
            spinner = actionPanel.Add(new Spinner() { CanFocus=false});
            timeLabel = actionPanel.Add(new Label() { Mode = LabelRenderMode.SingleLineAutoSize, Text = operation.StartTime.ToFriendlyPastTimeStamp().ToConsoleString() });

            
            spinner.IsSpinning = operation.State == OperationState.InProgress;

            foreach (var action in operation.Actions)
            {
                BindActionToActionPanel(action);
            }
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();

            Operation.Actions.Added.Subscribe(Actions_Added);
            Operation.Actions.Removed.Subscribe(Actions_Removed);

            Operation.Synchronize(nameof(ProgressOperation.Message), () =>
            {
                messageLabel.Text = Operation.Message;
            });

            Operation.Synchronize(nameof(ProgressOperation.State), () =>
            {
                if (Operation.State == OperationState.InProgress)
                {
                    spinner.IsSpinning = true;
                }
                else if (Operation.State == OperationState.Completed)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Green);
                }
                else if (Operation.State == OperationState.Failed)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Red);
                }
                else if (Operation.State == OperationState.CompletedWithWarnings)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.DarkYellow);
                }
                else if (Operation.State == OperationState.Queued)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Gray);
                }
                else if (Operation.State == OperationState.NotSet)
                {
                    spinner.IsSpinning = false;
                    spinner.Value = new ConsoleCharacter('?', backgroundColor: ConsoleColor.Gray);
                }
            });

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
