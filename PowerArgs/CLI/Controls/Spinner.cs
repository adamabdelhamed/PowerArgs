using System;
using System.Threading;

namespace PowerArgs.Cli
{
    public class Spinner : ConsoleControl
    {
        private static readonly TimeSpan spinTimeInterval = TimeSpan.FromMilliseconds(80);
        private static readonly char[] frames = "|/-\\".ToCharArray();

        private Timer spinTimerHandle;
        private int currentFrameIndex;

        public Spinner()
        {
            Width = 1;
            Height = 1;

            this.Added += Spinner_Added;
            this.Removed += Spinner_Removed;
            currentFrameIndex = 0;
        }

        private void Spinner_Added()
        {
            spinTimerHandle = Application.MessagePump.SetInterval(AdvanceFrame, spinTimeInterval);
        }

        private void Spinner_Removed()
        {
            Application.MessagePump.ClearInterval(spinTimerHandle);
        }

        private void AdvanceFrame()
        {
            currentFrameIndex = currentFrameIndex == frames.Length - 1 ? 0 : currentFrameIndex+1;
            Application?.Paint();
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(frames[currentFrameIndex], Foreground, Background);
            context.DrawPoint(0, 0);
        }
    }
}
