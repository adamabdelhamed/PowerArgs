using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class CliMessagePump
    {
        private Queue<Action> messages = new Queue<Action>();
        public bool IsRunning { get; private set; } = false;

        private bool stopRequested = false;

        public void QueueAction(Action a)
        {
            lock(messages)
            {
                messages.Enqueue(a);
            }
        }

        public void Start()
        {
            Task.Factory.StartNew(Pump);
        }

        public void Stop()
        {
            stopRequested = true;
            while (IsRunning)
            {
                Thread.Sleep(10);
            }
            stopRequested = false;
        }

        private void Pump()
        {
            IsRunning = true;
            while (IsRunning)
            {
                bool idle = false;
                lock (messages)
                {
                    while (messages.Count > 0)
                    {
                        idle = false;
                        messages.Dequeue().Invoke();
                    }
                }

                if (idle)
                {
                    Thread.Sleep(10);
                }
            }
        }
    }
}
