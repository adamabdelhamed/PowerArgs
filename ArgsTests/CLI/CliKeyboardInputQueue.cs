using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArgsTests.CLI
{
    public class HumanInputItem
    {
        private static Random rand = new Random();
        public ConsoleKeyInfo Key { get; private set; }
        public TimeSpan PreDelay { get; private set; }
        public TimeSpan PostDelay { get; private set; }

        public static TimeSpan NormalDelay
        {
            get
            {
                return TimeSpan.FromMilliseconds(rand.Next(100, 300));
            }
        }

        public HumanInputItem(ConsoleKeyInfo key, TimeSpan preDelay, TimeSpan postDelay)
        {
            this.Key = key;
            this.PreDelay = preDelay;
            this.PostDelay = postDelay;
        }
    }

    public class CliKeyboardInputQueue
    {
        private Queue<HumanInputItem> invisibleQueue = new Queue<HumanInputItem>();
        private Queue<HumanInputItem> visibleQueue = new Queue<HumanInputItem>();
        private Queue<HumanInputItem> inputQueue;

        public CliKeyboardInputQueue(bool simulateRealUser = false)
        {
            inputQueue = simulateRealUser ? invisibleQueue : visibleQueue;
        }

        public void SimulateUserNow()
        {
            Task.Factory.StartNew(() =>
            {
                inputQueue = visibleQueue;

                lock(invisibleQueue)
                {
                    while (invisibleQueue.Count > 0)
                    {
                        var next = invisibleQueue.Dequeue();
                        Thread.Sleep(next.PreDelay);
                        if (next.Key.KeyChar != '\u0000' || next.Key.Key != ConsoleKey.NoName)
                        {
                            Enqueue(next.Key);
                        }
                        Thread.Sleep(next.PostDelay);
                    }
                }
            });
        }
        
        public bool KeyAvailable
        {
            get
            {
                lock (visibleQueue)
                {
                    return visibleQueue.Where(i => i.Key.KeyChar != '\u0000' || i.Key.Key != ConsoleKey.NoName).Count() > 0;
                }
            }
        }

        public ConsoleKeyInfo ReadKey()
        {
            lock(visibleQueue)
            {
                var item = visibleQueue.Dequeue();

                while(item.Key.KeyChar == '\u0000' && item.Key.Key == ConsoleKey.NoName)
                {
                    item = visibleQueue.Dequeue();
                }
                return item.Key;
            }
        }

        public void Enqueue(string input)
        {
            lock(inputQueue)
            {
                foreach (var c in input)
                {
                    inputQueue.Enqueue(new HumanInputItem(new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false), TimeSpan.Zero, HumanInputItem.NormalDelay));
                }
            }
        }

        public void EnqueueDelay(TimeSpan delay)
        {
            lock (inputQueue)
            {
                inputQueue.Enqueue(new HumanInputItem(new ConsoleKeyInfo('\u0000', ConsoleKey.NoName, false, false, false), TimeSpan.Zero, delay));
            }
        }

        public void Enqueue(ConsoleKey key)
        {
            lock (inputQueue)
            {
                inputQueue.Enqueue(new HumanInputItem(new ConsoleKeyInfo('\u0000', key, false, false, false), TimeSpan.Zero, HumanInputItem.NormalDelay));
            }
        }

        public void Enqueue(ConsoleKeyInfo key)
        {
            lock (inputQueue)
            {
                inputQueue.Enqueue(new HumanInputItem(key, TimeSpan.Zero, HumanInputItem.NormalDelay));
            }
        }
    }
}
