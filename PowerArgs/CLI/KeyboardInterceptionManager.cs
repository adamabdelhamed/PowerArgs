using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that manages key input interception for a console app. This is used to handle
    /// key input that is not tied to a particular control.
    /// </summary>
    public class KeyboardInterceptionManager
    {
        private class HandlerContext
        {
            internal Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> NakedHandlers {  get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
            internal Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> AltHandlers { get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
            internal Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> ShiftHandlers { get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
            internal Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> ControlHandlers { get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
        }

        private Stack<HandlerContext> handlerStack;

        internal KeyboardInterceptionManager()
        {
            handlerStack = new Stack<HandlerContext>();
            handlerStack.Push(new HandlerContext());
        }

        internal bool TryIntercept(ConsoleKeyInfo keyInfo)
        {
            bool alt = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt);
            bool control = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control);
            bool shift = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift);
            bool noModifier = alt == false && shift == false && control == false;

            int handlerCount = 0;

            if(noModifier && handlerStack.Peek().NakedHandlers.ContainsKey(keyInfo.Key))
            {
                handlerStack.Peek().NakedHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            if(alt && handlerStack.Peek().AltHandlers.ContainsKey(keyInfo.Key))
            {
                handlerStack.Peek().AltHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            if (shift && handlerStack.Peek().ShiftHandlers.ContainsKey(keyInfo.Key))
            {
                handlerStack.Peek().ShiftHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            if (control && handlerStack.Peek().ControlHandlers.ContainsKey(keyInfo.Key))
            {
                handlerStack.Peek().ControlHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            return handlerCount > 0;
        }

        
        internal ILifetime PushUnmanaged(ConsoleKey key, ConsoleModifiers? modifier, Action<ConsoleKeyInfo> handler)
        {
            Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> target;

            if (modifier.HasValue == false) target = handlerStack.Peek().NakedHandlers;
            else if (modifier.Value.HasFlag(ConsoleModifiers.Alt)) target = handlerStack.Peek().AltHandlers;
            else if (modifier.Value.HasFlag(ConsoleModifiers.Shift)) target = handlerStack.Peek().ShiftHandlers;
            else if (modifier.Value.HasFlag(ConsoleModifiers.Control)) target = handlerStack.Peek().ControlHandlers;
            else throw new ArgumentException("Unsupported modifier: "+modifier.Value);

            Stack<Action<ConsoleKeyInfo>> targetStack;
            if(target.TryGetValue(key, out targetStack) == false)
            {
                targetStack = new Stack<Action<ConsoleKeyInfo>>();
                target.Add(key, targetStack);
            }

            targetStack.Push(handler);
            var lt = new Lifetime();
            lt.OnDisposed(() =>
            {
                targetStack.Pop();
                if (targetStack.Count == 0)
                {
                    target.Remove(key);
                }
            });

            return lt;
        }

        /// <summary>
        /// Pushes this handler onto its appropriate handler stack for the given lifetime
        /// </summary>
        /// <param name="key">the key ti handle</param>
        /// <param name="modifier">the modifier, or null if you want to handle the unmodified keypress</param>
        /// <param name="handler">the code to run when the key input is intercepted</param>
        /// <param name="manager">the lifetime of the handlers registration</param>
        public void PushForLifetime(ConsoleKey key, ConsoleModifiers? modifier, Action<ConsoleKeyInfo> handler, ILifetimeManager manager)
        {
           manager.OnDisposed(PushUnmanaged(key, modifier, handler));
        }

        /// <summary>
        /// Pushes this handler onto the appropriate handler stack
        /// </summary>
        /// <param name="key">the key ti handle</param>
        /// <param name="modifier">the modifier, or null if you want to handle the unmodified keypress</param>
        /// <param name="handler">the code to run when the key input is intercepted</param>
        /// <returns>A subscription that you should dispose when you no longer want this interception to happen</returns>
        public ILifetime PushUnmanaged(ConsoleKey key, ConsoleModifiers? modifier, Action handler)
        {
            return PushUnmanaged(key, modifier, (k) => { handler(); });
        }

        /// <summary>
        /// Pushes this handler onto its appropriate handler stack for the given lifetime
        /// </summary>
        /// <param name="key">the key ti handle</param>
        /// <param name="modifier">the modifier, or null if you want to handle the unmodified keypress</param>
        /// <param name="handler">the code to run when the key input is intercepted</param>
        /// <param name="manager">the lifetime of the handlers registration</param>
        public void PushForLifetime(ConsoleKey key, ConsoleModifiers? modifier, Action handler, ILifetimeManager manager)
        {
            PushForLifetime(key, modifier, (k) => { handler(); }, manager);
        }
    }
}
