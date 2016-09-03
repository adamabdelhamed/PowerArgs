using System;
using System.Threading;

namespace PowerArgs.Cli
{
    public class Spinner : PixelControl
    {
        private static readonly TimeSpan spinTimeInterval = TimeSpan.FromMilliseconds(80);
        private static readonly char[] frames = "|/-\\".ToCharArray();

        private Timer spinTimerHandle;
        private int currentFrameIndex;

        public bool IsSpinning {
            get { return Get<bool>(); }
            set
            {
                Set(value);
                if(value && Application != null && spinTimerHandle == null)
                {
                    StartSpinTimer();
                }
                else if(!value && Application != null &&spinTimerHandle != null)
                {
                    StopSpinTimer();
                }
            }
        }
        public Spinner()
        {
            currentFrameIndex = 0;
            this.AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this.LifetimeManager);
            this.RemovedFromVisualTree.SubscribeForLifetime(OnRemovedFromVisualTree, this.LifetimeManager);
        }

        private void OnAddedToVisualTree()
        {
            if(IsSpinning)
            {
                StartSpinTimer();
            }
        }

        private void OnRemovedFromVisualTree()
        {
            if(IsSpinning)
            {
                StopSpinTimer();
            }
        }

        private void StartSpinTimer()
        {
            spinTimerHandle = Application.SetInterval(AdvanceFrame, spinTimeInterval);
        }

        private void StopSpinTimer()
        {
            Application.ClearInterval(spinTimerHandle);
            spinTimerHandle = null;
        }

        private void AdvanceFrame()
        {
            if (IsSpinning)
            {
                currentFrameIndex = currentFrameIndex == frames.Length - 1 ? 0 : currentFrameIndex + 1;
                var val = frames[currentFrameIndex];
                Value = new ConsoleCharacter(val, Foreground, Background);
            }
        }
    }
}
