using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PowerArgs.Cli
{
    public class RichTextEditor : ObservableObject
    {
        private RichCommandLineContext Context { get; set; }
        private Dictionary<ConsoleKey, IKeyHandler> KeyHandlers { get; set; }
        private SpacebarKeyHandler SpacebarHandler { get; set; }
        public ConsoleHistoryManager HistoryManager { get; private set; }

        /// <summary>
        /// Gets or sets the highlighter used to highlight tokens as the user types
        /// </summary>
        public SimpleSyntaxHighlighter Highlighter { get; set; }

        /// <summary>
        /// Gets the tab hey handler.  This will let you plug in custom tab completion logic.
        /// </summary>
        public TabKeyHandler TabHandler { get; private set; }

        /// <summary>
        /// Gets a read only collection of currently registered key handlers.
        /// </summary>
        public ReadOnlyCollection<IKeyHandler> RegisteredKeyHandlers
        {
            get
            {
                return KeyHandlers.Values.Distinct().ToList().AsReadOnly();
            }
        }

        public bool ThrowOnSyntaxHighlightException { get; set; }

        /// <summary>
        /// Gets or sets the context assit provider that should be used for this reader
        /// </summary>
        public IContextAssistProvider ContextAssistProvider
        {
            get
            {
                return this.SpacebarHandler.ContextAssistProvider;
            }
            set
            {
                this.SpacebarHandler.ContextAssistProvider = value;
            }
        }

        public int CursorPosition
        {
            get
            {
                return Context.BufferPosition;
            }
            set
            {
                Context.BufferPosition = value;
            }
        }

        public ConsoleString CurrentValue
        {
            get
            {
                return new ConsoleString(Context.Buffer);
            }
            set
            {
                Context.Buffer.Clear();
                Context.Buffer.AddRange(value);
                Context.BufferPosition = 0;
                FirePropertyChanged(nameof(CurrentValue));
            }
        }




   



        public RichTextEditor()
        {
            this.HistoryManager = new ConsoleHistoryManager();
            Context = new RichCommandLineContext(this.HistoryManager);
            Context.DisableConsoleRefresh = true;
            Context.Console = new RichTextStateConsole(this);
            TabHandler = new TabKeyHandler();
            SpacebarHandler = new SpacebarKeyHandler();
            KeyHandlers = new Dictionary<ConsoleKey, IKeyHandler>();
            RegisterHandler(new EnterKeyHandler());
            RegisterHandler(new ArrowKeysHandler());
            RegisterHandler(new HomeAndEndKeysHandler());
            RegisterHandler(new BackspaceAndDeleteKeysHandler());
            RegisterHandler(SpacebarHandler);
            RegisterHandler(TabHandler);
        }

        /// <summary>
        /// Lets you register a custom key handler. You are responsible for ensuring that each key is only handled by one handler.  This method will throw if
        /// you try to add a duplicate key handler.
        /// </summary>
        /// <param name="handler">The handler to register</param>
        public void RegisterHandler(IKeyHandler handler)
        {
            foreach (var key in handler.KeysHandled)
            {
                KeyHandlers.Add(key, handler);
            }
        }

        /// <summary>
        /// Registers a keypress with the editor.
        /// </summary>
        /// <param name="key">The key press info</param>
        /// <param name="prototype">if specified, the foreground and background color will be taken from this prototype, otherwise the system defaults will be used</param>
        public void RegisterKeyPress(ConsoleKeyInfo key, ConsoleCharacter? prototype = null)
        {
            Context.Reset();
            Context.KeyPressed = key;
            Context.CharacterToWrite = new ConsoleCharacter(Context.KeyPressed.KeyChar,
                prototype.HasValue ? prototype.Value.ForegroundColor : ConsoleString.DefaultForegroundColor,
                prototype.HasValue ? prototype.Value.BackgroundColor : ConsoleString.DefaultBackgroundColor);

            IKeyHandler handler = null;

            if (KeyHandlers.TryGetValue(Context.KeyPressed.Key, out handler) == false && RichTextCommandLineReader.IsWriteable(Context.KeyPressed))
            {
                WriteCharacterForPressedKey(Context);
                DoSyntaxHighlighting(Context);
            }
            else if (handler != null)
            {
                handler.Handle(Context);

                if (Context.Intercept == false && RichTextCommandLineReader.IsWriteable(Context.KeyPressed))
                {
                    WriteCharacterForPressedKey(Context);
                }

                DoSyntaxHighlighting(Context);
            }
            FireValueChanged();
        }

        public void Clear()
        {
            Context.Buffer.Clear();
            CursorPosition = 0;
        }

        private void WriteCharacterForPressedKey(RichCommandLineContext context)
        {
            if (CursorPosition == Context.Buffer.Count)
            {
                Context.Buffer.Add(context.CharacterToWrite);
            }
            else
            {
                Context.Buffer.Insert(CursorPosition, context.CharacterToWrite);
            }

            CursorPosition++;
        }

        private void DoSyntaxHighlighting(RichCommandLineContext context)
        {
            if (Highlighter == null)
            {
                return;
            }

            bool highlightChanged = false;

            try
            {
                highlightChanged = Highlighter.TryHighlight(context);
            }
            catch (Exception ex)
            {
                if (ThrowOnSyntaxHighlightException)
                {
                    throw;
                }
            }

            if (highlightChanged)
            {
                FireValueChanged();
            }
        }

        private void FireValueChanged()
        {
            FirePropertyChanged(nameof(CurrentValue));
        }

        private class RichTextStateConsole : IConsoleProvider
        {
            private RichTextEditor state;
            public RichTextStateConsole(RichTextEditor state)
            {
                this.state = state;
            }

            public ConsoleColor BackgroundColor { get; set; }
            public ConsoleColor ForegroundColor { get; set; }

            public int BufferWidth { get; set; }

            public int WindowHeight { get; set; }
            public int WindowWidth { get; set; }
            public int CursorLeft
            {
                get
                {
                    return state.CursorPosition;
                }
                set
                {
                    state.CursorPosition = value;
                }
            }

            public int CursorTop
            {
                get
                {
                    return 0;
                }
                set
                {

                }
            }

            public bool KeyAvailable
            {
                get
                {
                    return false;
                }
            }

            public void Clear()
            {
                state.Clear();
            }

            public int Read()
            {
                throw new NotImplementedException();
            }

            public ConsoleKeyInfo ReadKey()
            {
                throw new NotImplementedException();
            }

            public ConsoleKeyInfo ReadKey(bool intercept)
            {
                throw new NotImplementedException();
            }

            public string ReadLine()
            {
                throw new NotImplementedException();
            }

            public void Write(ConsoleCharacter consoleCharacter)
            {
                throw new NotImplementedException();
            }

            public void Write(ConsoleString consoleString)
            {
                throw new NotImplementedException();
            }

            public void Write(object output)
            {
                throw new NotImplementedException();
            }

            public void WriteLine()
            {
                throw new NotImplementedException();
            }

            public void WriteLine(ConsoleString consoleString)
            {
                throw new NotImplementedException();
            }

            public void WriteLine(object output)
            {
                throw new NotImplementedException();
            }
        }
    }
}
