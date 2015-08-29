using System;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A utility that lets you display a progress bar on the console
    /// </summary>
    public class CliProgressBar
    {
        /// <summary>
        /// Gets or sets the character to write when drawing the border
        /// </summary>
        public ConsoleCharacter BorderPen { get; set; }

        /// <summary>
        /// Gets or sets the background color to use when filling in progress
        /// </summary>
        public ConsoleColor FillColor { get; set; }

        /// <summary>
        /// Gets or sets the foreground color to use when filling in progress
        /// </summary>
        public ConsoleColor MessageFillColor { get; set; }

        /// <summary>
        /// Gets or sets the progress.  This value should be between 0 and 1, both inclusive.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Gets the width, in characters of the progress bar control.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets or sets the message to display inside of the progress bar
        /// </summary>
        public ConsoleString Message { get; set; }

        /// <summary>
        /// Gets or sets the console to write to
        /// </summary>
        public IConsoleProvider Console { get; set; }


        private ConsoleSnapshot topLeft, messageStart;
        private ConsoleWiper wiper;
        private int indeterminateHighlightIndex;

        // internal for unit testing
        internal ConsoleString renderedMessage;

        /// <summary>
        /// Creates a new progress bar given a width
        /// </summary>
        /// <param name="initialMessage">an initial message to display in the progress bar</param>
        /// <param name="width">the width to use or null to use the default width which is one third of the console buffer width</param>
        public CliProgressBar(ConsoleString initialMessage = null, int? width = null)
        {
            Console = ConsoleProvider.Current;
            Message = initialMessage;
            Width = width.HasValue ? width.Value : Console.BufferWidth / 3;
            BorderPen = new ConsoleCharacter(' ', null, ConsoleColor.DarkGray);
            FillColor = ConsoleColor.Green;
            MessageFillColor = ConsoleColor.Black;
            indeterminateHighlightIndex = -1;
        }

        /// <summary>
        /// Creates a new progress bar given a width
        /// </summary>
        /// <param name="initialMessage">an initial message to display in the progress bar</param>
        /// <param name="width">the width to use or null to use the default width which is one third of the console buffer width</param>
        public CliProgressBar(string initialMessage, int? width = null) : this(initialMessage.ToConsoleString(), width) { }

        /// <summary>
        /// Renders the entire progress bar
        /// </summary>
        public void Render()
        {
            if(Console.CursorLeft > 0)
            {
                Console.WriteLine();
            }

            topLeft = Console.TakeSnapshot();
            messageStart = topLeft.CreateOffsetSnapshot(2, 1);
            wiper = new ConsoleWiper(topLeft);
            wiper.Bottom = wiper.Top + 2;
            wiper.Wipe();
            DrawBorder();
            Update();
        }

        /// <summary>
        /// Renders the progress bar and shows an indeterminate progress animation until the operation completes
        /// </summary>
        /// <param name="heartbeat">a function that should return true as long as you want to continue to block.  If you return false then this method will return.</param>
        /// <param name="pollingInterval">How fast you want the progress bar to call your heartbeat function</param>
        public void RenderAndPollIndeterminate(Func<bool> heartbeat, TimeSpan pollingInterval)
        {
            Render();
            indeterminateHighlightIndex = 0;
            bool cancelled = false;
            try
            {
                var bgTask = Task.Factory.StartNew(() =>
                {
                    while (cancelled == false)
                    {
                        Update();
                        indeterminateHighlightIndex++;
                        if (indeterminateHighlightIndex > Width - 4)
                        {
                            indeterminateHighlightIndex = 0;
                        }
                        Thread.Sleep(50);
                    }
                });

                while (heartbeat())
                {
                    Thread.Sleep(pollingInterval);
                }
            }
            finally
            {
                indeterminateHighlightIndex = -1;
                Update();
                cancelled = true;
            }
        }
        
        /// <summary>
        /// Renders the progress bar and shows an indeterminate progress animation until the Task completes.  This method will not
        /// start the task so it must be started somewhere else.
        /// </summary>
        /// <param name="workTask">the task to wait for.  This method will not start the task so it must be started somewhere else.</param>
        public void RenderUntilIndeterminate(Task workTask)
        {
            Render();
            indeterminateHighlightIndex = 0;
            bool cancelled = false;
            try
            {
                var animationTask = Task.Factory.StartNew(() =>
                {
                    while (cancelled == false)
                    {
                        Update();
                        indeterminateHighlightIndex++;
                        if (indeterminateHighlightIndex > Width - 4)
                        {
                            indeterminateHighlightIndex = 0;
                        }
                        Thread.Sleep(50);
                    }
                });

                workTask.Wait();
            }
            finally
            {
                indeterminateHighlightIndex = -1;
                Update();
                cancelled = true;
            }
        }

        /// <summary>
        /// Renders the progress bar and shows an indeterminate progress animation. Simultaneously, the work action is started. The bar will animate
        /// as long as the work action is running.
        /// </summary>
        /// <param name="workTask">the task to wait for</param>
        public void RenderUntilIndeterminate(Action workAction)
        {
            Task workTask = new Task(workAction);
            workTask.Start();
            RenderUntilIndeterminate(workTask);
        }

        /// <summary>
        /// Renders the progress bar and automatically updates it on a polling interval. The method blocks until the progress reaches 1 or
        /// your poll action throws an OperationCancelledException, whichever comes first.  It is expected that you will update the progress
        /// via the poll action.  You can also update the message during the poll action.
        /// </summary>
        /// <param name="pollAction">An action to run on each polling interval</param>
        /// <param name="pollingInterval">The polling interval</param>
        public void RenderAndPollDeterminate(Action pollAction, TimeSpan pollingInterval)
        {
            Render();

            try
            {
                while (Progress < 1)
                {
                    Thread.Sleep(pollingInterval);
                    pollAction();
                    Update();
                }
            }
            catch(OperationCanceledException)
            {
                Update();
            }
            finally
            {
                wiper.MoveCursorToLineAfterBottom();
            }
        }

        /// <summary>
        /// Clears the progress bar from the console and restores the console to the position it was in before drawing the progress bar
        /// </summary>
        public void Wipe()
        {
            wiper.Wipe();
            topLeft.Restore();
        }

        /// <summary>
        /// Renders the middle portion of the progress bar that contains the message and progress fill.  You must have called Render() ahead of time for this
        /// to make sense.
        /// </summary>
        public void Update()
        {
            var maxMessageLength = Width - 4;
            renderedMessage = Message;

            renderedMessage = renderedMessage.Replace("{%}", Math.Round(Progress * 100, 1) + " %");

            if (renderedMessage.Length > maxMessageLength)
            {
                renderedMessage = renderedMessage.Substring(0, maxMessageLength - 3) + "...";
            }

            while (renderedMessage.Length < maxMessageLength)
            {
                renderedMessage += " ";
            }

            if (indeterminateHighlightIndex < 0)
            {
                int toHighlight = (int)Math.Round(Progress * renderedMessage.Length);
                renderedMessage = renderedMessage.HighlightSubstring(0, toHighlight, MessageFillColor, FillColor);
            }
            else
            {
                renderedMessage = renderedMessage.HighlightSubstring(indeterminateHighlightIndex, 1, MessageFillColor, FillColor);
            }

            messageStart.Restore();
            Console.Write(renderedMessage);
            wiper.MoveCursorToLineAfterBottom();
        }

        private void DrawBorder()
        {
            topLeft.Restore();
            DrawHorizontalLine();
            if(Width < Console.BufferWidth)
            {
                Console.WriteLine();
            }
            Console.Write(BorderPen);
            Console.Write(BorderPen);
            Console.CursorLeft += Width - 4;
            Console.Write(BorderPen);
            Console.Write(BorderPen);

            if (Width < Console.BufferWidth)
            {
                Console.WriteLine();
            }

            DrawHorizontalLine();
        }

        private void DrawHorizontalLine()
        {
            for (int x = 0; x < Width; x++)
            {
                Console.Write(BorderPen);
            }
        }
    }
}
