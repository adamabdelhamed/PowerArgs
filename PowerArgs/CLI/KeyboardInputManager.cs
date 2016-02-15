using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public abstract class KeyboardInputManager
    {
        public IConsoleProvider Console { get; private set; }
        public KeyboardInputManager(IConsoleProvider console = null)
        {
            this.Console = console ?? ConsoleProvider.Current;
        }

        public bool TryProcessKeyboardInput()
        {
            if (this.Console.KeyAvailable)
            {
                var info = this.Console.ReadKey(true);
                return TryHandleKeyboardInput(info);
            }
            else
            {
                return false;
            }
        }

        public abstract bool TryHandleKeyboardInput(ConsoleKeyInfo key);
    }

    public enum KeyboardEventMatchMode
    {
        NoModifiers,
        AnyModifiers,
        Alt,
        Control,
        Shift
    }

    public class KeyboardEventHandler
    {
        public ConsoleKey Key { get; set; }
        public KeyboardEventMatchMode Mode { get; set; }

        internal string LookupKey
        {
            get
            {
                return Key + "---" + Mode;
            }
        }

        public static string CreateLookupKey(ConsoleKey key, KeyboardEventMatchMode mode)
        {
            return new KeyboardEventHandler() { Key = key, Mode = mode }.LookupKey;
        }
    }

    public class KeyboardEventContext
    {
        public bool AllowBubble { get; set; } = true;
    }

    public class KeyboardEvent
    {
        public ConsoleKeyInfo KeyInfo { get; }
    }

    public class ConsoleAppKeyboardInputManager : KeyboardInputManager
    {
        public ConsoleApp Application { get; private set; }

        private Stack<Dictionary<string, KeyboardEventHandler>> handlers;

        int currentStackDepth;

        public ConsoleAppKeyboardInputManager(ConsoleApp app)
        {
            this.Application = app;
            this.handlers = new Stack<Dictionary<string, KeyboardEventHandler>>();
            this.currentStackDepth = app.FocusManager.StackDepth;
            app.FocusManager.SubscribeForLifetime(nameof(FocusManager.StackDepth), StackDepthChanged, app.LifetimeManager);
        }

        private void StackDepthChanged()
        {
            if(Math.Abs(Application.FocusManager.StackDepth - currentStackDepth) != 1)
            {
                throw new InvalidOperationException("Stack depth changed by more than 1");
            }
            else if(Application.FocusManager.StackDepth == currentStackDepth + 1)
            {
                handlers.Push(new Dictionary<string, KeyboardEventHandler>());
            }
            else
            {
                handlers.Pop();
            }
        }

        public override bool TryHandleKeyboardInput(ConsoleKeyInfo key)
        {
            List<KeyboardEventHandler> potentialHandlers = new List<KeyboardEventHandler>();

            if(Application.FocusManager.FocusedControl != null)
            {
                Application.FocusManager.FocusedControl.HandleKeyInput()
            }

            if(key.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {

            }
            else if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {

            }
            else if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {

            }
            else
            {

            }

            handlers.Peek()[KeyboardEventHandler.CreateLookupKey(key.Key, KeyboardEventMatchMode.NoModifiers)];   
        }
    }
}
