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

        private Dialog(ConsoleControl content)
        {
            this.Width = content.Width;
            this.Height = content.Height;
            Controls.Add(content);
            Background = content.Background;
        }

        public override void OnAdd()
        {
            Application.GlobalKeyHandlers.Push(ConsoleKey.Escape, (key)=>
            {
                if(AllowEscapeToCancel)
                {
                    if (Cancelled != null) Cancelled();
                    ConsoleApp.Current.LayoutRoot.Controls.Remove(this);
                }
            });
        }

        public override void OnRemove()
        {
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Escape);
            Application.FocusManager.Pop();
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            context.Pen = new ConsoleCharacter(' ', null, Theme.DefaultTheme.FocusColor);
            context.DrawRect(0, 0, Width, Height);
        }

        public static void ConfirmYesOrNo(string message, Action yesCallback, Action noCallback = null)
        {
            ConfirmYesOrNo(message.ToConsoleString(), yesCallback, noCallback);
        }

        public static void ConfirmYesOrNo(ConsoleString message, Action yesCallback, Action noCallback = null)
        {
            Show(message, (b) =>
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

        public static void Show(ConsoleString message, Action<DialogButton> resultCallback, bool allowEscapeToCancel = true, params DialogButton [] buttons)
        {
            if(ConsoleApp.Current == null)
            {
                throw new InvalidOperationException("There is no console app running");
            }

            if(buttons.Length == 0)
            {
                throw new ArgumentException("You need to specify at least one button");
            }

            ConsolePanel content = new ConsolePanel();
    
            content.Width = ConsoleApp.Current.LayoutRoot.Width/2;
            content.Height = ConsoleApp.Current.LayoutRoot.Height/2;
            var dialog = new Dialog(content);
            dialog.AllowEscapeToCancel = allowEscapeToCancel;
            dialog.Cancelled += () => { resultCallback(null); };

            int totalButtonWidth = 0;

            int space = 2;
            foreach(var buttonInfo in buttons)
            {
                var myButtonInfo = buttonInfo;
                Button b = new Button() { Text = buttonInfo.DisplayText };
                b.Activated += () => 
                {
                    resultCallback(myButtonInfo);
                    ConsoleApp.Current.LayoutRoot.Controls.Remove(dialog);
                };
                content.Controls.Add(b);
                totalButtonWidth += b.Width+space;
            }
            totalButtonWidth -= space;

            int left = (content.Width - totalButtonWidth) / 2;

            Button firstButton = null;
            foreach(var button in content.Controls.Where(c=>c is Button).Select(c => c as Button))
            {
                button.X = left;
                button.Y = content.Height - 3;
                left += button.Width + space;
                firstButton = firstButton ?? button;
            }

            Label messageLabel = new Label() { Text = message, Width = content.Width - 4, X = 2, Y = 2 };
            content.Controls.Add(messageLabel);

            Layout.CenterVertically(ConsoleApp.Current.LayoutRoot, dialog);
            Layout.CenterHorizontally(ConsoleApp.Current.LayoutRoot, dialog);

            ConsoleApp.Current.FocusManager.Push();
            ConsoleApp.Current.LayoutRoot.Controls.Add(dialog);
            ConsoleApp.Current.FocusManager.TryMoveFocus();
            ConsoleApp.Current.Paint();
        }

        public static void ShowMessage(string message, Action doneCallback = null)
        {
            ShowMessage(message.ToConsoleString(), doneCallback);
        }

        public static void ShowMessage(ConsoleString message, Action doneCallback = null)
        {
            Show(message, (b) => { if (doneCallback != null) doneCallback(); },true, new DialogButton() { DisplayText = "ok" });
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
            TextBox inputBox = content.Add(new TextBox() { Y = 3, X = 2, Width = content.Width});

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
