using System;

namespace PowerArgs.Cli
{
    public class FrameRateMeter
    {
        private DateTime start;
        private DateTime currentSecond;
        private int framesInCurrentSecond;
        private int framesInPreviousSecond;

        public int TotalFrames { get; private set; }

        public int CurrentFPS
        {
            get
            {
                return framesInPreviousSecond;
            }
        }

        public FrameRateMeter()
        {
            start = DateTime.UtcNow;
            currentSecond = start;
            framesInCurrentSecond = 0;
        }

        public void Increment()
        {
            var now = DateTime.UtcNow;
            TotalFrames++;

            if(AreSameSecond(now, currentSecond))
            {
                framesInCurrentSecond++;
            }
            else
            {
                framesInPreviousSecond = framesInCurrentSecond;
                framesInCurrentSecond = 0;
                currentSecond = now;
            }
        }

        private bool AreSameSecond(DateTime a, DateTime b)
        {
            return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day && a.Hour == b.Hour && a.Minute == b.Minute && a.Second == b.Second;
        }
    }
}
