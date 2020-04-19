using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public abstract class CompactConsole : ConsolePanel
    {
        public TextBox InputBox { get; private set; }
        private CommandLineArgumentsDefinition def;
        private Label outputLabel;
        private Lifetime focusLt;
        public CompactConsole()
        {
            SubscribeForLifetime(nameof(Bounds),()=>HardRefresh(), this);
        }

        protected abstract CommandLineArgumentsDefinition CreateDefinition();
        protected virtual bool HasHistory() { return false; }
        protected virtual void AddHistory(string history) { }
        protected virtual ConsoleString GetHistoryPrevious() => throw new NotImplementedException();
        protected virtual ConsoleString GetHistoryNext() => throw new NotImplementedException();

        protected virtual Task Run(ArgAction toRun)
        {
            toRun.Invoke();
            WriteLine("Command finished".ToCyan());
            return Task.CompletedTask;
        }

        Lifetime refreshLt = new Lifetime();
        private void HardRefresh(ConsoleString outputValue = null)
        {
            refreshLt?.Dispose();
            refreshLt = new Lifetime();
            var myLt = refreshLt;
            Controls.Clear();
            if (Width < 10 || Height < 5) return;

            def = CreateDefinition();
            var gridLayout = Add(new GridLayout(new GridLayoutOptions()
            {
                Columns = new System.Collections.Generic.List<GridColumnDefinition>()
                {
                    new GridColumnDefinition(){ Type = GridValueType.Pixels, Width = 2 },           // 0 empty
                    new GridColumnDefinition(){ Type = GridValueType.RemainderValue, Width = 1 },   // 1 content
                    new GridColumnDefinition(){ Type = GridValueType.Pixels, Width = 2 },           // 2 empty
                },
                Rows = new System.Collections.Generic.List<GridRowDefinition>()
                {
                    new GridRowDefinition(){ Type = GridValueType.Pixels, Height = 1, },        // 0 empty
                    new GridRowDefinition(){ Type = GridValueType.Pixels, Height = 1, },        // 1 welcome message
                    new GridRowDefinition(){ Type = GridValueType.Pixels, Height = 1, },        // 2 press escape message
                    new GridRowDefinition(){ Type = GridValueType.Pixels, Height = 1, },        // 3 empty
                    new GridRowDefinition(){ Type = GridValueType.Pixels, Height = 1, },        // 4 input
                    new GridRowDefinition(){ Type = GridValueType.RemainderValue, Height = 1, },// 5 output
                    new GridRowDefinition(){ Type = GridValueType.Pixels, Height = 1, },        // 6 empty
                }
            }));
            gridLayout.Fill();
            gridLayout.RefreshLayout();
            var welcomePanel = gridLayout.Add(new ConsolePanel(), 1, 1);
            welcomePanel.Add(new Label() { Text = "Welcome to the console".ToWhite() }).CenterHorizontally();

            var escapePanel = gridLayout.Add(new ConsolePanel(), 1, 2);
            escapePanel.Add(new Label() { Text = "Press escape to resume".ToGray() }).CenterHorizontally();

            var inputPanel = gridLayout.Add(new ConsolePanel() { }, 1, 4);
            inputPanel.Add(new Label() { Text = "CMD> ".ToConsoleString() });
            InputBox = inputPanel.Add(new TextBox() { X = "CMD> ".Length, Width = inputPanel.Width - "CMD> ".Length, Foreground = ConsoleColor.Gray, Background = ConsoleColor.Black });
            InputBox.RichTextEditor.TabHandler.TabCompletionHandlers.Add(new PowerArgsRichCommandLineReader(def, new List<ConsoleString>(), false));
            ConsoleApp.Current.QueueAction(() =>
            {
                if (myLt == refreshLt)
                {


                    InputBox.Focused.SubscribeForLifetime(() =>
                    {
                        if (focusLt != null && focusLt.IsExpired == false && focusLt.IsExpiring == false)
                        {
                            focusLt.Dispose();
                        }

                        focusLt = new Lifetime();


                        Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab, null, () =>
                        {
                            var forgotten = OnHandleHey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
                        }, focusLt);
                        Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab, ConsoleModifiers.Shift, () =>
                        {
                            var forgotten = OnHandleHey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false));
                        }, focusLt);

                    }, refreshLt);

                    InputBox.Unfocused.SubscribeForLifetime(() =>
                    {
                        if (focusLt != null && focusLt.IsExpired == false && focusLt.IsExpiring == false)
                        {
                            focusLt.Dispose();
                        }
                    }, refreshLt);

                    InputBox.TryFocus();
                }
            });

            var outputPanel = gridLayout.Add(new ConsolePanel() { Background = ConsoleColor.Black }, 1, 5);
            outputLabel = outputPanel.Add(new Label() { Text = outputValue ?? UpdateAssistiveText(), Mode = LabelRenderMode.MultiLineSmartWrap }).Fill();

            InputBox.KeyInputReceived.SubscribeForLifetime(async (keyInfo)=>await OnHandleHey(keyInfo), InputBox);
        }

        private async Task OnHandleHey(ConsoleKeyInfo keyInfo)
        {
            if (InputBox.IsInputBlocked) return;

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                ConsoleString output = ConsoleString.Empty;
                try
                {
                    var args = Args.Convert(InputBox.Value.ToString());
                    AddHistory(InputBox.Value.ToString());

                    if(def.ExceptionBehavior?.Policy == ArgExceptionPolicy.StandardExceptionHandling)
                    {
                        def.ExceptionBehavior = new ArgExceptionBehavior(ArgExceptionPolicy.DontHandleExceptions);
                    }

                    ArgAction action;
                    ConsoleOutInterceptor.Instance.Attach();
                    try
                    {
                        action = Args.ParseAction(def, args);
                    }
                    finally
                    {
                        ConsoleOutInterceptor.Instance.Detatch();
                    }
                    InputBox.Dispose();
                    output = new ConsoleString(ConsoleOutInterceptor.Instance.ReadAndClear());

                    if (action.Cancelled == false)
                    {
                        await Run(action);
                    }
                }
                catch (Exception ex)
                {

                    var inner = ex;
                    if(ex is AggregateException && (ex as AggregateException).InnerExceptions.Count == 1)
                    {
                        inner = ex.InnerException;
                    }

                    if(ex is ArgException == false)
                    {
                        throw;
                    }

                    output = inner.Message.ToRed();
                }
                finally
                {
                    if (IsExpired == false)
                    {
                        HardRefresh(output);
                    }
                }
            }
            else if (keyInfo.Key == ConsoleKey.Tab)
            {
                ConsoleCharacter? prototype = InputBox.Value.Length == 0 ? (ConsoleCharacter?)null : InputBox.Value[InputBox.Value.Length - 1];
                InputBox.RichTextEditor.RegisterKeyPress(keyInfo, prototype);
            }
            else if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                if (HasHistory())
                {
                    InputBox.Value = GetHistoryPrevious();
                    outputLabel.Text = UpdateAssistiveText();
                }
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                if (HasHistory())
                {
                    InputBox.Value = GetHistoryNext();
                    outputLabel.Text = UpdateAssistiveText();
                }
            }
            else if (RichTextCommandLineReader.IsWriteable(keyInfo))
            {
                outputLabel.Text = UpdateAssistiveText();
            }
        }

        public void Write(ConsoleString text)
        {
            outputLabel.Text += text;
        }
        public void WriteLine(ConsoleString text) => Write(text + "\n");
        public void Clear() =>  outputLabel.Text = ConsoleString.Empty;

        private ConsoleString UpdateAssistiveText()
        {
            List<CommandLineAction> candidates;
            if (InputBox.Value.Length > 0)
            {
                var command = InputBox.Value.Split(" ".ToConsoleString()).FirstOrDefault();
                command = command ?? ConsoleString.Empty;
                candidates = def.Actions.Where(a => a.DefaultAlias.StartsWith(command.StringValue, StringComparison.OrdinalIgnoreCase)).ToList();

                if (candidates.Count == 0)
                {
                    return $"\nNo actions start with {InputBox.Value.ToString()}".ToRed();
                }
            }
            else
            {
                candidates = def.Actions;
            }

            List<ConsoleCharacter> buffer = new List<ConsoleCharacter>();
            buffer.AddRange("\n".ToConsoleString());
            foreach (var candidate in candidates)
            {
                buffer.AddRange(candidate.DefaultAlias.ToLower().ToCyan());
                buffer.AddRange(" - ".ToGray());
                buffer.AddRange(ConsoleString.Parse(candidate.Description));
                buffer.AddRange("\n".ToConsoleString());

                if (candidates.Count == 1)
                {
                    foreach (var arg in candidate.Arguments)
                    {
                        buffer.AddRange("    -".ToConsoleString() + arg.DefaultAlias.ToWhite());
                        buffer.AddRange(arg.IsRequired ? " * ".ToRed() : " - ".ToConsoleString());

                        if (string.IsNullOrEmpty(arg.Description) == false)
                        {
                            buffer.AddRange(ConsoleString.Parse(arg.Description));
                        }

                        buffer.AddRange("\n".ToConsoleString());

                        if (arg.IsEnum)
                        {
                            foreach (var enumString in arg.EnumValuesAndDescriptions)
                            {
                                buffer.AddRange(("     " + enumString + "\n").ToConsoleString());
                            }
                        }
                    }
                }

                buffer.AddRange("\n".ToConsoleString());
            }
            return new ConsoleString(buffer);
        }
    }
}
