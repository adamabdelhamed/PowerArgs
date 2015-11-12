using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class PumpExceptionArgs
    {
        public Exception Exception { get; private set; }
        public bool Handled { get; set; }

        internal PumpExceptionArgs(Exception ex)
        {
            this.Exception = ex;
            this.Handled = false;
        }
    }

    public class CliMessagePump
    {
        public event Action<PumpExceptionArgs> PumpException;

        private Queue<Func<Task>> messages = new Queue<Func<Task>>();
        private IConsoleProvider console;
        private Func<ConsoleKeyInfo, Task> keyInputHandler;
        private int managedCliThreadId;

        public bool IsRunning { get; private set; } = false;

        public CliMessagePump(IConsoleProvider console, Func<ConsoleKeyInfo, Task> keyInputHandler)
        {
            this.console = console;
            this.keyInputHandler = keyInputHandler;
        }

        public bool QueueRequired
        {
            get
            {
                if (IsRunning == false) throw new InvalidOperationException("The pump is not running");
                return Thread.CurrentThread.ManagedThreadId == managedCliThreadId;
            }
        }

        public void QueueAction(Action a)
        {
            if (QueueRequired == false)
            {
                a();
            }
            else
            {
                lock (messages)
                {
                    messages.Enqueue(async () =>
                    {
                        a();
                    });
                }
            }
        }

        public void QueueAction(Func<Task> asyncTask)
        {
            lock (messages)
            {
                messages.Enqueue(asyncTask);
            }
        }

        public Timer SetInterval(Action a, TimeSpan interval)
        {
            return SetInterval(async () =>
            {
                a();

            }, interval);
        }

        public Timer SetInterval(Func<Task> asyncTask, TimeSpan interval)
        {
            var ret = new Timer((o) =>
            {
                QueueAction(asyncTask);
            }, null, (int)interval.TotalMilliseconds, (int)interval.TotalMilliseconds);
            return ret;
        }

        public void ClearInterval(Timer t)
        {
            t.Change(Timeout.Infinite, Timeout.Infinite);
            t.Dispose();
        }

        public void ClearTimeout(Timer t)
        {
            t.Change(Timeout.Infinite, Timeout.Infinite);
            t.Dispose();
        }

        public Task Start()
        {
            var pumpTask = Task.Factory.StartNew(Pump);
            while (IsRunning == false)
            {
                Thread.Sleep(10);
            }
            return pumpTask;
        }


        private async Task Pump()
        {
            var syncCtx = new CliSynchronizationContext(messages);
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            managedCliThreadId = Thread.CurrentThread.ManagedThreadId;
            IsRunning = true;
            while (true)
            {
                bool idle = true;
                Func<Task> work = null;
                lock (messages)
                {
                    if (messages.Count > 0)
                    {
                        idle = false;
                        work = messages.Dequeue();
                    }
                }

                if (work != null)
                {
                    var t = work();
                    if (t.Exception != null && PumpException == null)
                    {
                        ExceptionDispatchInfo.Capture(t.Exception).Throw();
                    }
                    else if (t.Exception != null)
                    {
                        PumpExceptionArgs exceptionArgs = new PumpExceptionArgs(t.Exception);
                        PumpException(exceptionArgs);
                        if (exceptionArgs.Handled == false)
                        {
                            ExceptionDispatchInfo.Capture(t.Exception).Throw();
                        }
                    }
                }

                if (idle && this.console.KeyAvailable)
                {
                    var info = this.console.ReadKey(true);
                    if (info.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    QueueAction(async () => { await this.keyInputHandler(info); });
                }
                else if (idle)
                {
                    Thread.Sleep(10);
                }
            }
            IsRunning = false;
        }
    }

    public sealed class CliSynchronizationContext : SynchronizationContext
    {
        Queue<Func<Task>> asyncTasks;
        public CliSynchronizationContext(Queue<Func<Task>> asyncTasks)
        {
            this.asyncTasks = asyncTasks;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (asyncTasks)
            {
                asyncTasks.Enqueue(async () => 
                {
                    d.Invoke(state);
                });
            }
        }
        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException();
        }
    }
}
