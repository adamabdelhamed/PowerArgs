using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class KeyboardInterceptionManager
    {
        private class HandlerContext
        {
            public Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> NakedHandlers {  get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
            public Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> AltHandlers { get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
            public Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> ShiftHandlers { get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
            public Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>> ControlHandlers { get; private set; } = new Dictionary<ConsoleKey, Stack<Action<ConsoleKeyInfo>>>();
        }

        private Stack<HandlerContext> handlerStack;

        public KeyboardInterceptionManager()
        {
            handlerStack = new Stack<HandlerContext>();
            handlerStack.Push(new HandlerContext());
        }

        public Lifetime Push()
        {
            PushInternal();
            var ret = new Lifetime();
            ret.LifetimeManager.Manage(new Subscription(() =>
            {
                PopInternal();
            }));
            return ret;
        }

        private void PushInternal()
        {
            handlerStack.Push(new HandlerContext());
        }

        private void PopInternal()
        {
            handlerStack.Pop();
        }

        public bool TryIntercept(ConsoleKeyInfo keyInfo)
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

        
        public Subscription PushUnmanaged(ConsoleKey key, ConsoleModifiers? modifier, Action<ConsoleKeyInfo> handler)
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

        public Subscription PushUnmanaged(ConsoleKey key, ConsoleModifiers? modifier, Action handler)
        {
            return PushUnmanaged(key, modifier, (k) => { handler(); });
        }

        public void PushForLifetime(ConsoleKey key, ConsoleModifiers? modifier, Action handler, LifetimeManager manager)
        {
            PushForLifetime(key, modifier, (k) => { handler(); }, manager);
        }
    }
}
