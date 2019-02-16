using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Information about an option to be presented on a dialog
    /// </summary>
    public class DialogOption
    {
        /// <summary>
        /// The display text for the option
        /// </summary>
        public ConsoleString DisplayText { get; set; }

        /// <summary>
        /// The id of this option's value
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// An object that this option represents
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Compares the ids of each option
        /// </summary>
        /// <param name="obj">the other option</param>
        /// <returns>true if the ids match</returns>
        public override bool Equals(object obj)
        {
            var b = obj as DialogOption;
            if (b == null) return false;
            return b.Id == this.Id;
        }

        /// <summary>
        /// gets the hashcode of the id
        /// </summary>
        /// <returns>the hashcode of the id</returns>
        public override int GetHashCode() => Id == null ? base.GetHashCode() : Id.GetHashCode();
    }

    /// <summary>
    /// The base dialog options for controlling the dialog behavior
    /// </summary>
    public abstract class DialogOptions
    {
        /// <summary>
        /// The max height of the dialog. If set to 0 the dialog
        /// will take up most of the application layout root.
        /// </summary>
        public int MaxHeight { get; set; } = 8;

        /// <summary>
        /// If true (which is the default), the escape key can be used to dismiss the dialog
        /// which should be interpreted as a cancellation
        /// </summary>
        public bool AllowEscapeToCancel { get; set; } = true;

        internal abstract ConsoleControl GetContent();
    }

    /// <summary>
    /// Dialog options where you manually set the content of the dialog
    /// </summary>
    public class ControlDialogOptions : DialogOptions
    {
        /// <summary>
        /// The content of the dialog
        /// </summary>
        public ConsoleControl Content { get; set; }
        internal override ConsoleControl GetContent() => Content;
    }

    /// <summary>
    /// A flavor of dialog options where the dialog should present a text box
    /// for text input
    /// </summary>
    public class RichTextDialogOptions : DialogOptions
    {
        /// <summary>
        /// The message to display above the text box
        /// </summary>
        public ConsoleString Message { get; set; }

        /// <summary>
        /// The text box to use. It is optional. If none is provided then
        /// a text box will be created for you. You should provide it yourself
        /// if you want to show a default value or if you need to preconfigure the
        /// text box in some way
        /// </summary>
        public TextBox TextBox { get; set; }

        internal override ConsoleControl GetContent()
        {
            ConsolePanel content = new ConsolePanel();
            content.Width = ConsoleApp.Current.LayoutRoot.Width / 2;
            content.Height = ConsoleApp.Current.LayoutRoot.Height / 2;

            Label messageLabel = content.Add(new Label() { Text = Message, X = 2, Y = 2 });
            TextBox = TextBox ?? new TextBox() { Foreground = ConsoleColor.Black, Background = ConsoleColor.White };
            content.Add(TextBox).CenterHorizontally();
            TextBox.Y = 4;

            content.SynchronizeForLifetime(nameof(content.Bounds), () => { TextBox.Width = content.Width - 4; }, content);

            TextBox.KeyInputReceived.SubscribeForLifetime((k) =>
            {
                if (k.Key == ConsoleKey.Enter)
                {
                    Dialog.Dismiss();
                }
            }, TextBox);

            TextBox.AddedToVisualTree.SubscribeOnce(()=> TextBox.Application.QueueAction(()=>TextBox.TryFocus()));
            return content;
        }
    }

    /// <summary>
    /// Defines the mode for presenting dialog button options to the user
    /// </summary>
    public enum DialogButtonsPresentationMode
    {
        /// <summary>
        /// Displays as horizontally stacked buttons toward the bottom of the dialog. This is good
        /// if there are a very small number of options.
        /// </summary>
        Buttons,
        /// <summary>
        /// Displays options in a grid which can scale to handle a fairly large set of options
        /// </summary>
        Grid
    }

    /// <summary>
    /// A flavor of dialog options that you can use to show the user a set of options to choose from.
    /// </summary>
    public class DialogButtonOptions : DialogOptions
    {
        /// <summary>
        /// A generic Yes button
        /// </summary>
        public static DialogOption Yes => new DialogOption() { Id = "yes", DisplayText = "Yes".ToConsoleString() };
        /// <summary>
        /// A generic No button
        /// </summary>
        public static DialogOption No => new DialogOption() { Id = "no", DisplayText = "No".ToConsoleString() };
        /// <summary>
        /// A generic OK button
        /// </summary>
        public static DialogOption OK => new DialogOption() { Id = "ok", DisplayText = "OK".ToConsoleString() };

        /// <summary>
        /// The mode for displaying the options, defaults to buttons
        /// </summary>
        public DialogButtonsPresentationMode Mode { get; set; } = DialogButtonsPresentationMode.Buttons;

        /// <summary>
        /// The message to display above the options
        /// </summary>
        public ConsoleString Message { get; set; }

        /// <summary>
        /// The options
        /// </summary>
        public List<DialogOption> Options { get; set; }

        internal DialogOption SelectedOption { get; set; }

        internal override ConsoleControl GetContent()
        {
            if (Options == null || Options.Count == 0)
            {
                throw new ArgumentException("You need to specify at least one button");
            }

            if (Mode == DialogButtonsPresentationMode.Buttons)
            {
                ConsolePanel dialogContent = new ConsolePanel();
                ScrollablePanel messagePanel = dialogContent.Add(new ScrollablePanel()).Fill(padding: new Thickness(0, 0, 1, 3));
                Label messageLabel = messagePanel.ScrollableContent.Add(new Label() { Mode = LabelRenderMode.MultiLineSmartWrap, Text = Message }).FillHorizontally(padding: new Thickness(3, 3, 0, 0));
                StackPanel buttonPanel = dialogContent.Add(new StackPanel() { Margin = 1, Height = 1, Orientation = Orientation.Horizontal }).FillHorizontally(padding: new Thickness(1, 0, 0, 0)).DockToBottom(padding: 1);

                foreach (var option in Options)
                {
                    var myOption = option;
                    Button b = new Button() { Text = option.DisplayText };
                    b.Pressed.SubscribeOnce(() =>
                    {
                        SelectedOption = myOption;
                        Dialog.Dismiss();
                    });
                    buttonPanel.Controls.Add(b);
                }

                buttonPanel.Controls.Last().AddedToVisualTree.SubscribeOnce(() => buttonPanel.Application.QueueAction(()=>{ buttonPanel.Controls.Last().TryFocus(); }));
                return dialogContent;
            }
            else
            {
                Grid optionsGrid = new Grid(Options.Select(o => o as object).ToList());
                optionsGrid.MoreDataMessage = "More options below".ToYellow();
                optionsGrid.EndOfDataMessage = "End of menu";

                optionsGrid.VisibleColumns.Remove(optionsGrid.VisibleColumns.Where(v => v.ColumnName.ToString() == nameof(DialogOption.Id)).Single());
                optionsGrid.VisibleColumns[0].WidthPercentage = 1;
                optionsGrid.VisibleColumns[0].ColumnDisplayName = Message.IsUnstyled ? Message.ToYellow() : Message;
                optionsGrid.VisibleColumns[0].OverflowBehavior = new TruncateOverflowBehavior();
                (optionsGrid.VisibleColumns[0].OverflowBehavior as TruncateOverflowBehavior).ColumnWidth = 0;
                optionsGrid.SelectedItemActivated += () =>
                {
                    this.SelectedOption = optionsGrid.SelectedItem as DialogOption;
                    Dialog.Dismiss();
                };

                optionsGrid.AddedToVisualTree.SubscribeOnce(() => optionsGrid.Application.QueueAction(() => { optionsGrid.TryFocus(); }));

                return optionsGrid;
            }
        }
    }

    /// <summary>
    /// A console control that shows a dialog as a layer above a ConsoleApp
    /// </summary>
    public class Dialog : ConsolePanel
    {
        private DialogOptions options;
        private Button closeButton;
        private int myFocusStackDepth;

        /// <summary>
        /// Gets the topmost dialog on the current app
        /// </summary>
        public static Dialog Current => ConsoleApp.Current?.LayoutRoot.Controls.WhereAs<Dialog>().LastOrDefault();
        
        private Dialog(DialogOptions options)
        {
            this.options = options;
            Add(options.GetContent()).Fill(padding: new Thickness(0, 0, 1, 1));
            closeButton = Add(new Button() { Text = "Close (ESC)".ToConsoleString(),Background = DefaultColors.H1Color, Foreground = ConsoleColor.Black }).DockToRight(padding: 1);
            closeButton.Pressed.SubscribeForLifetime(Escape, this);
            BeforeAddedToVisualTree.SubscribeForLifetime(OnBeforeAddedToVisualTree, this);
            AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this);
            RemovedFromVisualTree.SubscribeForLifetime(OnRemovedFromVisualTree, this);
        }

        /// <summary>
        /// Shows the dialog on top of the current app
        /// </summary>
        /// <returns>a promise that resolves when this dialog is dismissed. This promise never rejects.</returns>
        private Promise Show()
        {
            ConsoleApp.AssertAppThread();
            var deferred = Deferred.Create();
            ConsoleApp.Current.LayoutRoot.Add(this);
            RemovedFromVisualTree.SubscribeForLifetime(deferred.Resolve, this);
            return deferred.Promise;
        }

        private void OnBeforeAddedToVisualTree()
        {
            Application.FocusManager.Push();
            myFocusStackDepth = Application.FocusManager.StackDepth;
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, Escape, this );
        }

        private void OnAddedToVisualTree()
        {
            if(Parent != Application.LayoutRoot)
            {
                throw new InvalidOperationException("Dialogs must be added to the LayoutRoot of an application");
            }

            if (options.MaxHeight > 0)
            {
                this.Height = Math.Min(options.MaxHeight, Application.LayoutRoot.Height - 2);
            }
            else
            {
                this.Height = Application.LayoutRoot.Height - 2;
            }

            this.CenterVertically();
            this.FillHorizontally();
            ConsoleApp.Current.FocusManager.TryMoveFocus();
            Application.FocusManager.SubscribeForLifetime(nameof(FocusManager.StackDepth), () =>
            {
                if(Application.FocusManager.StackDepth != myFocusStackDepth)
                {
                    closeButton.Background = DefaultColors.DisabledColor;
                }
                else
                {
                    closeButton.Background = DefaultColors.H1Color;
                }
            }, this);

        }

        private void OnRemovedFromVisualTree()
        {
            Application.FocusManager.Pop();
        }

        private void Escape()
        {
            if (options.AllowEscapeToCancel)
            {
                ConsoleApp.Current.LayoutRoot.Controls.Remove(this);
            }
        }

        /// <summary>
        /// Paints the dialog control
        /// </summary>
        /// <param name="context">the drawing surface</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(' ', null, myFocusStackDepth == Application.FocusManager.StackDepth ? DefaultColors.H1Color : DefaultColors.DisabledColor);
            context.DrawLine(0, 0, Width, 0);
            context.DrawLine(0, Height-1, Width, Height-1);
            base.OnPaint(context);
        }

        /// <summary>
        /// Dismisses the top most dialog on the current ConsoleApp
        /// </summary>
        public static void Dismiss()
        {
            var dialogToDismiss = Dialog.Current;
            if (dialogToDismiss == null)
            {
                throw new InvalidOperationException("There is no dialog to dismiss");
            }
            else
            {
                ConsoleApp.Current.LayoutRoot.Controls.Remove(dialogToDismiss);
            }
        }

        /// <summary>
        /// Shows a dialog using the specified options
        /// </summary>
        /// <param name="options">the options that configure the dialog's behavior</param>
        /// <returns></returns>
        public static Promise Show(ControlDialogOptions options) => new Dialog(options).Show();

        /// <summary>
        /// Shows a dialog with a message and a set of options for the user to choose from
        /// </summary>
        /// <param name="options">the options used to configure the dialog</param>
        /// <returns>a promise that resolves with the selected option or null if the dialog was cancelled. The promise never rejects.</returns>
        public static Promise<DialogOption> ShowMessage(DialogButtonOptions options)
        {
            var d = Deferred<DialogOption>.Create();
            var rawPromise = new Dialog(options).Show();
            rawPromise.Then(() => d.Resolve(options.SelectedOption));
            return d.Promise;
        }

        /// <summary>
        /// Shows a dialog with a message and an ok button
        /// </summary>
        /// <param name="message">the message to show</param>
        /// <returns>a promise that resolves when the dialog is dismissed. The promise never rejects.</returns>
        public static Promise ShowMessage(ConsoleString message)
        {
            var d = Deferred.Create();
            var buttonPromise = ShowMessage(new DialogButtonOptions()
            {
                Message = message,
                Options = new List<DialogOption>() { DialogButtonOptions.OK }
            });
            buttonPromise.Then((button) => d.Resolve());
            return d.Promise;
        }

        /// <summary>
        /// Shows a dialog with a message and an ok button
        /// </summary>
        /// <param name="message">the message to show</param>
        /// <returns>a promise that resolves when the dialog is dismissed. The promise never rejects.</returns>
        public static Promise ShowMessage(string message) => ShowMessage(message.ToConsoleString());

        /// <summary>
        /// Shows a dialog with the given message and provides the user with a yes and no option
        /// </summary>
        /// <param name="message">the message to show</param>
        /// <returns>a promise that resolves if the yes option was cicked. it rejects if no was clicked or if the dialog was cancelled</returns>
        public static Promise ShowYesConfirmation(string message) => ShowYesConfirmation(message.ToConsoleString());

        /// <summary>
        /// Shows a dialog with the given message and provides the user with a yes and no option
        /// </summary>
        /// <param name="message">the message to show</param>
        /// <returns>a promise that resolves if the yes option was cicked. it rejects if no was clicked or if the dialog was cancelled</returns>
        public static Promise ShowYesConfirmation(ConsoleString message)
        {
            var d = Deferred.Create();
            var buttonPromise = ShowMessage(new DialogButtonOptions()
            {
                Message = message,
                Options = new List<DialogOption>()
                {
                    DialogButtonOptions.Yes,
                    DialogButtonOptions.No
                }
            });
            buttonPromise.Then((button) =>
            {
                if (button != null && button.Equals(DialogButtonOptions.Yes))
                {
                    d.Resolve();
                }
                else
                {
                    d.Reject(new Exception("No was selected"));
                }
            });
            return d.Promise;
        }

        /// <summary>
        /// Shows a message and lets the user pick from a set of options defined by an enum
        /// </summary>
        /// <typeparam name="T">the enum type</typeparam>
        /// <param name="message">the message to show</param>
        /// <returns>A promise that resolves with the selected value or null if the dialog was cancelled. The promise never rejects.</returns>
        public static Promise<T?> ShowEnumOptions<T>(ConsoleString message) where T : struct
        {
            var d = Deferred<T?>.Create();
            var rawPromise = ShowEnumOptions(message, typeof(T));
            rawPromise.Then((o) => d.Resolve(o == null ? new T?() : (T)o));
            return d.Promise;
        }

        /// <summary>
        /// Shows a message and lets the user pick from a set of options defined by an enum
        /// </summary>
        /// <param name="message">the message to show</param>
        /// <param name="enumType">the enum type</param>
        /// <returns>A promise that resolves with the selected value or null if the dialog was cancelled. The promise never rejects.</returns>
        public static Promise<object> ShowEnumOptions(ConsoleString message, Type enumType)
        {
            Deferred<object> deferred = Deferred<object>.Create();
            var rawPromise = ShowMessage(new DialogButtonOptions()
            {
                Message = message,
                Mode = DialogButtonsPresentationMode.Grid,
                Options = Enums.GetEnumValues(enumType).OrderBy(e => e.ToString()).Select(e => new DialogOption() { Id = e.ToString(), DisplayText = e.ToString().ToConsoleString(), Value = e }).ToList()
            });

            rawPromise.Then((b) =>
            {
                if (b == null) deferred.Resolve(null);
                else deferred.Resolve(b.Value);
            });

            return deferred.Promise;
        }

        /// <summary>
        /// Shows a dialog that presents the user with a message and a text box
        /// </summary>
        /// <param name="options">the options used to configure the dialog</param>
        /// <returns>a promise that resolves with the value of the text box at the time of dismissal. This promise never rejects.</returns>
        public static Promise<ConsoleString> ShowRichTextInput(RichTextDialogOptions options)
        {
            var d = Deferred<ConsoleString>.Create();
            var rawPromise = new Dialog(options).Show();
            rawPromise.Then(() => d.Resolve(options.TextBox.Value));
            return d.Promise;
        }
    }
}
