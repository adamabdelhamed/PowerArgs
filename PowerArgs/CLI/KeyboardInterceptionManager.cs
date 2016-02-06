using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class KeyboardInterceptionManager
    {
        private Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> nakedHandlers = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
        private Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> altHandlers = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
        private Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> shiftHandlers = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
        private Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> controlHandlers = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();

        public bool TryIntercept(ConsoleKeyInfo keyInfo)
        {
            bool alt = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt);
            bool control = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control);
            bool shift = keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift);
            bool noModifier = alt == false && shift == false && control == false;

            int handlerCount = 0;

            if(noModifier && nakedHandlers.ContainsKey(keyInfo.Key))
            {
                nakedHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            if(alt && altHandlers.ContainsKey(keyInfo.Key))
            {
                altHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            if (shift && shiftHandlers.ContainsKey(keyInfo.Key))
            {
                shiftHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            if (control && controlHandlers.ContainsKey(keyInfo.Key))
            {
                controlHandlers[keyInfo.Key].Peek().Invoke(keyInfo);
                handlerCount++;
            }

            return handlerCount > 0;
        }

        
        public Subscription PushUnmanaged(ConsoleKey key, ConsoleModifiers? modifier, Action<ConsoleKeyInfo> handler)
        {
            Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> target;

            if (modifier.HasValue == false) target = nakedHandlers;
            else if (modifier.Value.HasFlag(ConsoleModifiers.Alt)) target = altHandlers;
            else if (modifier.Value.HasFlag(ConsoleModifiers.Shift)) target = shiftHandlers;
            else if (modifier.Value.HasFlag(ConsoleModifiers.Control)) target = controlHandlers;
            else throw new ArgumentException("Unsupported modifier: "+modifier.Value);

            Stack<Action<ConsoleKeyInfo>> targetStack;
            if(target.TryGetValue(key, out targetStack) == false)
            {
                targetStack = new Stack<Action<ConsoleKeyInfo>>();
                target.Add(key, targetStack);
            }

            targetStack.Push(handler);
            var sub = new Subscription(() => 
            {
                targetStack.Pop();
                if(targetStack.Count == 0)
                {
                    target.Remove(key);
                }
            });
            return sub;
        }

        public void PushForLifetime(ConsoleKey key, ConsoleModifiers? modifier, Action<ConsoleKeyInfo> handler, LifetimeManager manager)
        {
           manager.Manage(PushUnmanaged(key, modifier, handler));
        }

        public void Push(ConsoleKey key, ConsoleModifiers? modifier, Action<ConsoleKeyInfo> handler)
        {
            PushForLifetime(key, modifier, handler, LifetimeManager.AmbientLifetimeManager);
        }


        public Subscription PushUnmanaged(ConsoleKey key, ConsoleModifiers? modifier, Action handler)
        {
            return PushUnmanaged(key, modifier, (k) => { handler(); });
        }

        public void PushForLifetime(ConsoleKey key, ConsoleModifiers? modifier, Action handler, LifetimeManager manager)
        {
            PushForLifetime(key, modifier, (k) => { handler(); }, manager);
        }

        public void Push(ConsoleKey key, ConsoleModifiers? modifier, Action handler)
        {
            Push(key, modifier, (k) => { handler(); });
        }
    }
}
