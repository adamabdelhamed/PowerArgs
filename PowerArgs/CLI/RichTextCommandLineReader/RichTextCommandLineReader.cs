using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A utility that lets you prompt a console user for input in an interactive way.  It provides hooks for tab completion, syntax highlighting, history management via the up and down arrows, etc.
    /// </summary>
    public class RichTextCommandLineReader
    {
        /// <summary>
        /// An event that fires after the user enters a key.  The event will not fire for
        /// terminating keystrokes
        /// </summary>
        public event Action<RichCommandLineContext> AfterReadKey;

        /// <summary>
        /// The console implementation to target
        /// </summary>
        public IConsoleProvider Console { get; set; }

        private Dictionary<ConsoleKey, IKeyHandler> KeyHandlers { get; set; }

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

        /// <summary>
        /// Gets or sets the highlighter used to highlight tokens as the user types
        /// </summary>
        public SimpleSyntaxHighlighter Highlighter { get; set; }

        /// <summary>
        /// Gets the tab hey handler.  This will let you plug in custom tab completion logic.
        /// </summary>
        public TabKeyHandler TabHandler { get; private set; }

        internal SpacebarKeyHandler SpacebarHandler { get; private set; }

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

        /// <summary>
        /// Gets the history manager.  This will let you add your historical command line values so that end users can cycle through them using the up and down arrows.
        /// </summary>
        public ConsoleHistoryManager HistoryManager { get; private set; }

        /// <summary>
        /// Gets or sets whether or not to propagate exceptions thrown by syntax highlighters.  The default is false.
        /// </summary>
        public bool ThrowOnSyntaxHighlightException { get; set; }

        /// <summary>
        /// Gets or sets the object to use to synchronize reading with other threads that are interacting with the console
        /// </summary>
        public object SyncLock { get; set; }

        /// <summary>
        /// Creates a new reader.
        /// </summary>
        public RichTextCommandLineReader()
        {
            Console = ConsoleProvider.Current;
            HistoryManager = new ConsoleHistoryManager();
            SyncLock = new object();
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
            foreach(var key in handler.KeysHandled)
            {
                KeyHandlers.Add(key, handler);
            }
        }

        /// <summary>
        /// Unregisters the given key handler from the reader.  You should only do this if you're planning on overriding the default handlers, and you should do so
        /// with caution.
        /// </summary>
        /// <param name="handler">The handler to unregister</param>
        public void UnregisterHandler(IKeyHandler handler)
        {
            foreach(var key in handler.KeysHandled)
            {
                KeyHandlers.Remove(key);
            }
        }

        /// <summary>
        /// Unregisters the handler for the given key
        /// </summary>
        /// <param name="key">the key to unregister</param>
        /// <returns>true if there was a handler registered and removed, false otherwise</returns>
        public bool UnregisterHandler(ConsoleKey key)
        {
            return KeyHandlers.Remove(key);
        }

        /// <summary>
        /// Reads a line of text from the console and converts it into a string array that has accounted for escape sequences and quoted string literals.
        /// </summary>
        /// <param name="initialBuffer">Optionally seed the prompt with an initial value that the end user can modify</param>
        /// <returns>the command line that was read</returns>
        public string[] ReadCommandLine(ConsoleString initialBuffer = null)
        {
            var line = ReadLine(initialBuffer).ToString();
            var ret = Args.Convert(line);
            return ret;
        }

        /// <summary>
        /// Reads a line of text from the console.  Any interactions you've configured before calling this method will be in effect.
        /// </summary>
        /// <param name="initialBuffer">Optionally seed the prompt with an initial value that the end user can modify</param>
        /// <returns>a line of text from the console</returns>
        public ConsoleString ReadLine(ConsoleString initialBuffer = null)
        {
            RichCommandLineContext context;
            lock (SyncLock)
            {
                context = new RichCommandLineContext(this.HistoryManager);
                context.Console = this.Console;
                context.ConsoleStartTop = this.Console.CursorTop;
                context.ConsoleStartLeft = this.Console.CursorLeft;
            }

            if(initialBuffer != null)
            {
                lock (SyncLock)
                {
                    context.ReplaceConsole(initialBuffer);
                }
            }

            while (true)
            {
                context.Reset();
                context.KeyPressed = this.Console.ReadKey(true);
                lock (SyncLock)
                {
                    context.CharacterToWrite = new ConsoleCharacter(context.KeyPressed.KeyChar);
                    context.BufferPosition = this.Console.CursorLeft - context.ConsoleStartLeft + (this.Console.CursorTop - context.ConsoleStartTop) * this.Console.BufferWidth;

                    if(context.BufferPosition < 0 || context.BufferPosition > context.Buffer.Count)
                    {
                        var message = string.Format("The cursor is not located within the bounds of the buffer. Cursor: {0},{1}    Start position: {2},{3}    Buffer position: {4}    Buffer length: {5}", this.Console.CursorTop, this.Console.CursorLeft, context.ConsoleStartTop, context.ConsoleStartLeft, context.BufferPosition, context.Buffer.Count);
                        try
                        {
                            throw new IndexOutOfRangeException(message);
                        }
                        catch(Exception ex)
                        {
                            PowerLogger.LogLine(ex.ToString());
                            throw;
                        }
                    }

                    IKeyHandler handler = null;

                    if (KeyHandlers.TryGetValue(context.KeyPressed.Key, out handler) == false && IsWriteable(context.KeyPressed))
                    {
                        context.WriteCharacterForPressedKey();
                        DoSyntaxHighlighting(context);
                    }
                    else if (handler != null)
                    {
                        handler.Handle(context);

                        if (context.Intercept == false && IsWriteable(context.KeyPressed))
                        {
                            context.WriteCharacterForPressedKey();
                        }

                        DoSyntaxHighlighting(context);

                        if (context.IsFinished)
                        {
                            this.Console.WriteLine();
                            break;
                        }
                    }

                    if (AfterReadKey != null)
                    {
                        AfterReadKey(context);
                    }
                }
            }

            return new ConsoleString(context.Buffer);
        }

        /// <summary>
        /// Determines if the given key is writable text
        /// </summary>
        /// <param name="info">the key info</param>
        /// <returns>true if the given key is writable text, false otherwise</returns>
        public static bool IsWriteable(ConsoleKeyInfo info)
        {
            if (info.KeyChar == '\u0000') return false;
            if (info.Key == ConsoleKey.Escape) return false;

            return true;
        }

        private void DoSyntaxHighlighting(RichCommandLineContext context)
        {
            if(Highlighter == null)
            {
                return;
            }

            bool highlightChanged = false;

            try
            {
                highlightChanged = Highlighter.TryHighlight(context);
            }
            catch(Exception ex)
            {
                if (ThrowOnSyntaxHighlightException)
                {
                    throw;
                }
                else
                {
                    PowerLogger.LogLine("Syntax highlighting threw exception: " + ex.ToString());
                }
            }

            if(highlightChanged)
            {
                context.RefreshConsole(0, 0);
            }
        }
    }
}
