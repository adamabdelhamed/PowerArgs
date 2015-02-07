using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// A utility that lets you prompt a console user for input in an interactive way.  It provides hooks for tab completion, syntax highlighting, history management via the up and down arrows, etc.
    /// </summary>
    public class RichTextCommandLineReader
    {
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

        /// <summary>
        /// Gets the history manager.  This will let you add your historical command line values so that end users can cycle through them using the up and down arrows.
        /// </summary>
        public ConsoleHistoryManager HistoryManager { get; private set; }

        /// <summary>
        /// Creates a new reader.
        /// </summary>
        public RichTextCommandLineReader()
        {
            Console = new StdConsoleProvider();
            HistoryManager = new ConsoleHistoryManager();

            TabHandler = new TabKeyHandler();

            KeyHandlers = new Dictionary<ConsoleKey, IKeyHandler>();
            RegisterHandler(new EnterKeyHandler());
            RegisterHandler(new ArrowKeysHandler());
            RegisterHandler(new HomeAndEndKeysHandler());
            RegisterHandler(new BackspaceAndDeleteKeysHandler());
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
            RichCommandLineContext context = new RichCommandLineContext(this.HistoryManager);
            context.Console = this.Console;
            context.ConsoleStartTop = this.Console.CursorTop;
            context.ConsoleStartLeft = this.Console.CursorLeft;

            if(initialBuffer != null)
            {
                context.ReplaceConsole(initialBuffer);
            }

            while (true)
            {
                context.Reset();
                context.KeyPressed = this.Console.ReadKey(true);
                context.CharacterToWrite = new ConsoleCharacter(context.KeyPressed.KeyChar);
                context.BufferPosition = this.Console.CursorLeft - context.ConsoleStartLeft + (this.Console.CursorTop - context.ConsoleStartTop) * this.Console.BufferWidth;

                IKeyHandler handler = null;

                if (KeyHandlers.TryGetValue(context.KeyPressed.Key, out handler) == false && context.CharacterToWrite.Value != '\u0000')
                {
                    context.WriteCharacterForPressedKey();
                    DoSyntaxHighlighting(context);
                }
                else if(handler != null)
                {
                    handler.Handle(context);

                    if (context.Intercept == false && context.CharacterToWrite.Value != '\u0000')
                    {
                        this.Console.Write(context.CharacterToWrite);
                    }

                    DoSyntaxHighlighting(context);

                    if (context.IsFinished)
                    {
                        this.Console.WriteLine();
                        break;
                    }
                }
            }

            return new ConsoleString(context.Buffer);
        }

        private void DoSyntaxHighlighting(RichCommandLineContext context)
        {
            if(Highlighter == null)
            {
                return;
            }

            bool highlightChanged = Highlighter.TryHighlight(context);

            if(highlightChanged)
            {
                context.RefreshConsole(0, 0);
            }
        }
    }
}
