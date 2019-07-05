using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public abstract class CompactConsole : ConsolePanel
    {
        private TextBox tb;
        private CommandLineArgumentsDefinition def;
        protected Label outputLabel;

        public CompactConsole()
        {
            SubscribeForLifetime(nameof(Bounds),()=>HardRefresh(), this);
        }

        protected abstract CommandLineArgumentsDefinition CreateDefinition();
        protected virtual bool HasHistory() { return false; }
        protected virtual void AddHistory(string history) { }
        protected virtual ConsoleString GetHistoryPrevious() => throw new NotImplementedException();
        protected virtual ConsoleString GetHistoryNext() => throw new NotImplementedException();

        protected virtual Task<ConsoleString> Run(ArgAction toRun)
        {
            toRun.Invoke();
            return Task.FromResult("Command finished".ToCyan());
        }

        private void HardRefresh(ConsoleString outputValue = null)
        {
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
            tb = inputPanel.Add(new TextBox() { X = "CMD> ".Length, Width = inputPanel.Width - "CMD> ".Length, Foreground = ConsoleColor.Gray, Background = ConsoleColor.Black });
            tb.RichTextEditor.TabHandler.TabCompletionHandlers.Add(new PowerArgsRichCommandLineReader(def, new List<ConsoleString>(), false));
            tb.TryFocus();

            var outputPanel = gridLayout.Add(new ConsolePanel() { Background = ConsoleColor.Black }, 1, 5);
            outputLabel = outputPanel.Add(new Label() { Text = outputValue ?? UpdateAssistiveText(), Mode = LabelRenderMode.MultiLineSmartWrap }).Fill();

            tb.KeyInputReceived.SubscribeForLifetime(async (keyInfo)=>await OnHandleHey(keyInfo), tb);
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab, null, () =>
            {
                var forgotten = OnHandleHey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
            }, tb);
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab, ConsoleModifiers.Shift, () =>
            {
                var forgotten = OnHandleHey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false));
            }, tb);
        }

        private async Task OnHandleHey(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                ConsoleString output = ConsoleString.Empty;
                try
                {
                    var args = Args.Convert(tb.Value.ToString());
                    AddHistory(tb.Value.ToString());
                    var action = Args.ParseAction(def, args);
                    (tb.Parent as ConsolePanel).Controls.Remove(tb);
                    output = await Run(action);
                }
                catch (Exception ex)
                {
                    output = ex.Message.ToRed();
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
                ConsoleCharacter? prototype = tb.Value.Length == 0 ? (ConsoleCharacter?)null : tb.Value[tb.Value.Length - 1];
                tb.RichTextEditor.RegisterKeyPress(keyInfo, prototype);
            }
            else if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                if (HasHistory())
                {
                    tb.Value = GetHistoryPrevious();
                }
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                if (HasHistory())
                {
                    tb.Value = GetHistoryNext();
                }
            }
            else if (RichTextCommandLineReader.IsWriteable(keyInfo))
            {
                outputLabel.Text = UpdateAssistiveText();
            }
        }

        private ConsoleString UpdateAssistiveText()
        {
            List<CommandLineAction> candidates;
            if (tb.Value.Length > 0)
            {
                var command = tb.Value.Split(" ".ToConsoleString()).FirstOrDefault();
                candidates = def.Actions.Where(a => a.DefaultAlias.StartsWith(command.StringValue, StringComparison.OrdinalIgnoreCase)).ToList();

                if (candidates.Count == 0)
                {
                    return $"\nNo actions start with {tb.Value.ToString()}".ToRed();
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
