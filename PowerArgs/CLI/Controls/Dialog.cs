using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class DialogButton
    {
        public string DisplayText { get; set; }
        public string Id { get; set; }
    }

    // todo - restrict the focus system to include only the dialog buttons so the dialog feels more modal
    public class Dialog : ConsolePanel
    {
        public bool AllowEscapeToCancel { get; set; }

        public event Action Cancelled;

        private Button closeButton;

        public Dialog(ConsoleControl content)
        {
            Add(content).Fill(padding: new Thickness(0, 0, 1, 1));
            closeButton = Add(new Button() { Text = "Close (ESC)",Background = Theme.DefaultTheme.H1Color, Foreground = ConsoleColor.Black }).DockToRight(padding: 1);
            closeButton.Activated += Escape;
        }

        public override void OnBeforeAddedToVisualTree()
        {
            base.OnBeforeAddedToVisualTree();
            Application.FocusManager.Push();
            Application.GlobalKeyHandlers.Push(ConsoleKey.Escape, (key) =>
            {
                Escape();
            });
        }

        public override void OnAddedToVisualTree()
        {
            if(Parent != Application.LayoutRoot)
            {
                throw new InvalidOperationException("Dialogs must be added to the LayoutRoot of an application");
            }

            this.Fill(padding: new Thickness(0, 0, 2, 2));
            ConsoleApp.Current.FocusManager.TryMoveFocus();
        }

        private void Escape()
        {
            if (AllowEscapeToCancel)
            {
                if (Cancelled != null) Cancelled();
                ConsoleApp.Current.LayoutRoot.Controls.Remove(this);
            }
        }

        public override void OnRemovedFromVisualTree()
        {
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Escape);
            Application.FocusManager.Pop();
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(' ', null, Theme.DefaultTheme.H1Color);
            context.DrawLine(0, 0, Width, 0);
            context.DrawLine(0, Height-1, Width, Height-1);
            base.OnPaint(context);
        }

        public static void ConfirmYesOrNo(string message, Action yesCallback, Action noCallback = null)
        {
            ConfirmYesOrNo(message.ToConsoleString(), yesCallback, noCallback);
        }

        public static void ConfirmYesOrNo(ConsoleString message, Action yesCallback, Action noCallback = null)
        {
            ShowMessage(message, (b) =>
            {
                if (b != null && b.Id == "y")
                {
                    yesCallback();
                }
                else if (noCallback != null)
                {
                    noCallback();
                }
            }, true, new DialogButton() { Id = "y", DisplayText = "Yes", }, new DialogButton() { Id = "n", DisplayText = "No" });
        }

        public static void ShowMessage(ConsoleString message, Action<DialogButton> resultCallback, bool allowEscapeToCancel = true, params DialogButton [] buttons)
        {
            if(buttons.Length == 0)
            {
                throw new ArgumentException("You need to specify at least one button");
            }

            ConsolePanel dialogContent = new ConsolePanel();

            Dialog dialog = new Dialog(dialogContent);
            dialog.AllowEscapeToCancel = allowEscapeToCancel;
            dialog.Cancelled += () => { resultCallback(null); };

            ScrollablePanel messagePanel = dialogContent.Add(new ScrollablePanel()).Fill(padding: new Thickness(0, 0, 1, 3));
            Label messageLabel = messagePanel.ScrollableContent.Add(new Label() { Mode = LabelRenderMode.MultiLineSmartWrap, Text = message }).FillHoriontally(padding: new Thickness(3,3,0,0) );

            StackPanel buttonPanel = dialogContent.Add(new StackPanel() { Margin = 1, Height = 1, Orientation = Orientation.Horizontal }).FillHoriontally(padding: new Thickness(1,0,0,0)).DockToBottom(padding: 1);

            Button firstButton = null;
            foreach (var buttonInfo in buttons)
            {
                var myButtonInfo = buttonInfo;
                Button b = new Button() { Text = buttonInfo.DisplayText };
                b.Activated += () => 
                {
                    ConsoleApp.Current.LayoutRoot.Controls.Remove(dialog);
                    resultCallback(myButtonInfo);
                };
                buttonPanel.Controls.Add(b);
                firstButton = firstButton ?? b;
            }
            ConsoleApp.Current.LayoutRoot.Controls.Add(dialog);
        }


        public static void ShowMessage(string message, Action doneCallback = null)
        {
            ShowMessage(message.ToConsoleString(), doneCallback);
        }

        public static void ShowMessage(ConsoleString message, Action doneCallback = null)
        {
            ShowMessage(message, (b) => { if (doneCallback != null) doneCallback(); },true, new DialogButton() { DisplayText = "ok" });
        }

        public static void ShowTextInput(ConsoleString message, Action<ConsoleString> resultCallback, Action cancelCallback = null, bool allowEscapeToCancel = true)
        {
            if (ConsoleApp.Current == null)
            {
                throw new InvalidOperationException("There is no console app running");
            }

            ConsolePanel content = new ConsolePanel();

            content.Width = ConsoleApp.Current.LayoutRoot.Width / 2;
            content.Height = ConsoleApp.Current.LayoutRoot.Height / 2;

            var dialog = new Dialog(content);
            dialog.AllowEscapeToCancel = allowEscapeToCancel;
            dialog.Cancelled += () => { if (cancelCallback != null) cancelCallback(); };

     

            Label messageLabel = content.Add(new Label() { Text = message, Width = content.Width - 4, X = 2, Y = 2 });
            TextBox inputBox = content.Add(new TextBox() { Y = 5, Width = content.Width - 4, Background = ConsoleColor.White}).CenterHorizontally();

            inputBox.KeyInputReceived += (key) =>
            {
                if(key.Key == ConsoleKey.Enter)
                {
                    resultCallback(inputBox.Value);
                    ConsoleApp.Current.LayoutRoot.Controls.Remove(dialog);
                }
            };

            Layout.CenterVertically(ConsoleApp.Current.LayoutRoot, dialog);
            Layout.CenterHorizontally(ConsoleApp.Current.LayoutRoot, dialog);

            ConsoleApp.Current.FocusManager.Push();
            ConsoleApp.Current.LayoutRoot.Controls.Add(dialog);
            inputBox.TryFocus();
            ConsoleApp.Current.Paint();
        }
    }
}
