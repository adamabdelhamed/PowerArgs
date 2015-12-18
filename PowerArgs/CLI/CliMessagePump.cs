using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that defines the exception handling contract for a ConsoleApp
    /// </summary>
    public class PumpExceptionArgs
    {
        /// <summary>
        /// Gets the exception that a handler may handle
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets whether or not this exception has been handled by a handler.  Your event handler should set this to true
        /// if you want the pump to continue processing messages.  If no handler sets this to true then the pump thread will throw
        /// and the process will crash.
        /// </summary>
        public bool Handled { get; set; }

        internal PumpExceptionArgs(Exception ex)
        {
            this.Exception = ex;
            this.Handled = false;
        }
    }

    /// <summary>
    /// A class that defines a message that can be executed by the message pump
    /// </summary>
    public class PumpMessage
    {
        /// <summary>
        /// The maximum value of the idempotency ids used to dedupe idempotent messages
        /// </summary>
        public const int MaxIdempotencyId = 255;

        /// <summary>
        /// A description for this message, used for debugging
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the idempotency Id of this message which is used to dedupe idempotent messaes
        /// </summary>
        public int IdempotencyId { get; private set; }

        private Action pumpAction;

        /// <summary>
        /// Creates a pump message with the given action and idempotency id.  
        /// </summary>
        /// <param name="a">The action to execute when this message is dequeued by a message pump</param>
        /// <param name="idempotencyId">An Id used to dedupe idempotent messages.  Any value less than 0 means that the message will be processed no matter what.  
        /// If the value is greater than zero then when the message pump queue contains multiple messages with the same value then only the first message in the 
        /// queue will be processed. For the very special PaintMessage, the system will ensure that message is processed at the end of the messages currently in the queue</param>
        /// <param name="description">A description of this message that can be used for debugging purposes</param>
        public PumpMessage(Action a, int idempotencyId = -1, string description = "Not specified")
        {
            if(idempotencyId > MaxIdempotencyId)
            {
                throw new ArgumentOutOfRangeException("idempotencyId must be less than "+MaxIdempotencyId);
            }

            if(idempotencyId == PaintMessage.IdempotencyId && GetType() != typeof(PaintMessage))
            {
                throw new InvalidOperationException("The idempotency id " + idempotencyId + " can only be used by a " + nameof(PaintMessage));
            }

            this.Description = description;
            this.IdempotencyId = idempotencyId;
            this.pumpAction = a;
        }

        /// <summary>
        /// Executes the action associated with this message
        /// </summary>
        internal void Execute()
        {
            if(pumpAction != null)
            {
                pumpAction();
            }
        }
    }

    /// <summary>
    /// A message that indicates a paint request that gets processed differently from other message types to reduce
    /// the number of times the message pump processes paint requests for a console app
    /// </summary>
    internal class PaintMessage : PumpMessage
    {
        public const int IdempotencyId = 1;
        public const string PaintDescription = nameof(PaintMessage);
        public PaintMessage(Action paintAction) : base(paintAction, IdempotencyId, PaintDescription) { }
    }

    /// <summary>
    /// A class that is used to manage a CLI thread in a similar way that other platforms synchronize work
    /// on a UI thread
    /// </summary>
    public class CliMessagePump
    {
        private class StopPumpMessage : PumpMessage
        {
            public StopPumpMessage() : base(() => { }, description: "Stops the pump") { }
        }

        /// <summary>
        /// An event that fires when a pump message throws an exception while executing.  Handlers can mark the exception as handled
        /// if they want to keep the pump running.  If no handler is registered or no handler marks the exception as handled then the
        /// pump thread will throw and the process will crash.
        /// </summary>
        public event Action<PumpExceptionArgs> PumpException;

        public event Action WindowResized;

        /// <summary>
        /// A boolean that can be checked to see if the pump is currently running
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        private Queue<PumpMessage> pumpMessageQueue = new Queue<PumpMessage>();
        private IConsoleProvider console;
        private Action<ConsoleKeyInfo> keyInputHandler;
        private int managedCliThreadId;
        private int lastConsoleWidth;

        /// <summary>
        /// Creates a new message pump given a console to use for keyboard input
        /// </summary>
        /// <param name="console">the console to use for keyboard input</param>
        /// <param name="keyInputHandler">a key event handler to use to process keyboard input</param>
        public CliMessagePump(IConsoleProvider console, Action<ConsoleKeyInfo> keyInputHandler)
        {
            this.console = console;
            this.lastConsoleWidth = this.console.BufferWidth;
            this.keyInputHandler = keyInputHandler;
        }

        /// <summary>
        /// Queues the given action for processing on the pump thread
        /// </summary>
        /// <param name="a">the action that will be processed in order on the pump thread</param>
        public void QueueAction(Action a)
        {
            var pumpMessage = new PumpMessage(a);
            QueueAction(pumpMessage);
        }

        public void Stop()
        {
            if(IsRunning)
            {
                QueueAction(new StopPumpMessage());
            }
        }

        /// <summary>
        /// Queues the given message for processing on the pump thread
        /// </summary>
        /// <param name="pumpMessage">the message that will be processed in order on the pump thread</param>
        public void QueueAction(PumpMessage pumpMessage)
        {
            lock (pumpMessageQueue)
            {
                pumpMessageQueue.Enqueue(pumpMessage);
            }
        }

        /// <summary>
        /// Schedules the given action for periodic processing by the message pump
        /// </summary>
        /// <param name="a">The action to schedule for periodic processing</param>
        /// <param name="interval">the execution interval for the action</param>
        /// <returns>A timer that can be passed to ClearInterval if you want to cancel the work</returns>
        public Timer SetInterval(Action a, TimeSpan interval)
        {
            var ret = new Timer((o) =>
            {
                QueueAction(a);
            }, null, (int)interval.TotalMilliseconds, (int)interval.TotalMilliseconds);
            return ret;
        }

        /// <summary>
        /// Schedules the given action for a one time execution after the given period elapses
        /// </summary>
        /// <param name="a">The action to schedule</param>
        /// <param name="period">the period of time to wait before executing the action</param>
        /// <returns></returns>
        public Timer SetTimeout(Action a, TimeSpan period)
        {
            var ret = new Timer((o) =>
            {
                QueueAction(a);
            }, null, (int)period.TotalMilliseconds,Timeout.Infinite);
            return ret;
        }

        /// <summary>
        /// Cancels the scheduled execution of a periodic action given the timer that was provided by SetInterval.  The timer will be disposed.
        /// </summary>
        /// <param name="t">the timer given by SetInterval</param>
        public void ClearInterval(Timer t)
        {
            t.Change(Timeout.Infinite, Timeout.Infinite);
            t.Dispose();
        }

        /// <summary>
        /// Cancels the scheduled execution of a one time action given the timer that was provided by SetTimeout.  The timer will be disposed.
        /// </summary>
        /// <param name="t">The timer given by SetTimeout</param>
        public void ClearTimeout(Timer t)
        {
            t.Change(Timeout.Infinite, Timeout.Infinite);
            t.Dispose();
        }

        /// <summary>
        /// Starts the message pump which will begin processing messages
        /// </summary>
        /// <returns>A task that will complete when the message pump starts</returns>
        internal Task Start()
        {
            var pumpTask = Task.Factory.StartNew(Pump);
            while (IsRunning == false)
            {
                Thread.Sleep(10);
            }
            return pumpTask;
        }


        private void Pump()
        {
            IsRunning = true;
            bool stopRequested = false;
            while (true)
            {
                if(lastConsoleWidth != this.console.BufferWidth && WindowResized != null)
                {
                    DebounceResize();
                    WindowResized();
                }

                bool idle = true;
                lock (pumpMessageQueue)
                {
                    DedupeWorkQueue();
                    while (pumpMessageQueue.Count > 0)
                    {
                        idle = false;
                        var message = pumpMessageQueue.Dequeue();
                        if (message is StopPumpMessage)
                        {
                            stopRequested = true;
                            break;
                        }
                        else
                        {
                            TryWork(message);
                        }
                    }
                }

                if(stopRequested)
                {
                    break;
                }

                if (this.console.KeyAvailable)
                {
                    idle = false;
                    var info = this.console.ReadKey(true);
                    QueueAction(() => { this.keyInputHandler(info); });
                }
                
                if(idle)
                {
                    Thread.Sleep(10);
                }
            }

            IsRunning = false;
        }

        private void TryWork(PumpMessage work)
        {
            try
            {
                work.Execute();
            }
            catch (Exception ex)
            {
                bool handled = false;

                if (PumpException != null)
                {
                    PumpExceptionArgs exceptionArgs = new PumpExceptionArgs(ex);
                    PumpException(exceptionArgs);
                    handled = exceptionArgs.Handled;
                }

                if (handled == false)
                {
                    throw;
                }
            }
        }

        private void DebounceResize()
        {
            console.Clear();
            bool done = false;
            ActionDebouncer debouncer = new ActionDebouncer(TimeSpan.FromSeconds(.25), () =>
            {
                done = true;
            });

            debouncer.Trigger();
            while(done == false)
            {
                if(console.BufferWidth != lastConsoleWidth)
                {
                    lastConsoleWidth = console.BufferWidth;
                    debouncer.Trigger();
                }
            }
        }

        private void DedupeWorkQueue()
        {
            if (pumpMessageQueue.Count < 2)
            {
                return;
            }

            Queue<PumpMessage> newQueue = new Queue<PumpMessage>();
            bool[] dedupeBuffer = new bool[PumpMessage.MaxIdempotencyId+1];
            PumpMessage paintMessage = null;
            while(pumpMessageQueue.Count > 0)
            {
                var message = pumpMessageQueue.Dequeue();
                if(message.IdempotencyId < 0 || dedupeBuffer[message.IdempotencyId] == false)
                {
                    if (message is PaintMessage)
                    {
                        paintMessage = message;
                    }
                    else
                    {
                        newQueue.Enqueue(message);
                    }
                    if (message.IdempotencyId >= 0)
                    {
                        dedupeBuffer[message.IdempotencyId] = true;
                    }
                }
                else
                {
                    // yay, we deduped an idempotent message like a paint operation
                }
            }

            if(paintMessage != null)
            {
                newQueue.Enqueue(paintMessage);
            }

            pumpMessageQueue = newQueue;
        }
    }
}
