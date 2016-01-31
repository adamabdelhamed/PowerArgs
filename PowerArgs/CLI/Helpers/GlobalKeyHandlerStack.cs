using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class GlobalKeyHandlerStack
    {
        private class Handler
        {
            public Action<ConsoleKeyInfo> HandlerAction { get; private set; }

            public Handler(Action<ConsoleKeyInfo> handlerAction)
            {
                this.HandlerAction = handlerAction;
            }
        }

        private Dictionary<ConsoleKey, Stack<Handler>> handlers;

        private Dictionary<ConsoleKey, Stack<Handler>> altHandlers;

        public GlobalKeyHandlerStack()
        {
            handlers = new Dictionary<ConsoleKey, Stack<Handler>>();
            altHandlers = new Dictionary<ConsoleKey, Stack<Handler>>();
        }

        public void Push(ConsoleKey key, Action<ConsoleKeyInfo> handler, bool altModifier = false)
        {
            Dictionary<ConsoleKey, Stack<Handler>> dictionary = altModifier ? altHandlers : handlers;
            Stack<Handler> handlerStack;
            if(dictionary.TryGetValue(key, out handlerStack) == false)
            {
                handlerStack = new Stack<Handler>();
                dictionary.Add(key, handlerStack);
            }

            handlerStack.Push(new Handler(handler));
        }

        public bool TryHandle(ConsoleKeyInfo info)
        {
            Dictionary<ConsoleKey, Stack<Handler>> dictionary = info.Modifiers.HasFlag(ConsoleModifiers.Alt) ? altHandlers : handlers;

            Stack<Handler> handlerStack;
            if (dictionary.TryGetValue(info.Key, out handlerStack) == false)
            {
                return false;
            }

            if(handlerStack.Count == 0)
            {
                return false;
            }

            handlerStack.Peek().HandlerAction(info);
            return true;
        }

        public void Pop(ConsoleKey key, bool altModifier = false)
        {
            Dictionary<ConsoleKey, Stack<Handler>> dictionary = altModifier ? altHandlers : handlers;

            Stack<Handler> handlerStack = dictionary[key];
            handlerStack.Pop();
        }
    }
}
