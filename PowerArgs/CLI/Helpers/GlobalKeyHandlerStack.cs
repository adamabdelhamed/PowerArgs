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

        public GlobalKeyHandlerStack()
        {
            handlers = new Dictionary<ConsoleKey, Stack<Handler>>();
        }

        public void Push(ConsoleKey key, Action<ConsoleKeyInfo> handler)
        {
            Stack<Handler> handlerStack;
            if(handlers.TryGetValue(key, out handlerStack) == false)
            {
                handlerStack = new Stack<Handler>();
                handlers.Add(key, handlerStack);
            }

            handlerStack.Push(new Handler(handler));
        }

        public bool TryHandle(ConsoleKeyInfo info)
        {
            Stack<Handler> handlerStack;
            if (handlers.TryGetValue(info.Key, out handlerStack) == false)
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

        public void Pop(ConsoleKey key)
        {
            Stack<Handler> handlerStack = handlers[key];
            handlerStack.Pop();
        }
    }
}
