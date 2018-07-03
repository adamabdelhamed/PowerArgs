using System;
using System.Threading;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that animates (or spins). It can be used to let the user know that the system
    /// is working when they are required to wait
    /// </summary>
    public class Spinner : PixelControl
    {
        private static readonly TimeSpan spinTimeInterval = TimeSpan.FromMilliseconds(80);
        private static readonly char[] frames = "|/-\\".ToCharArray();

        private IDisposable spinTimerHandle;
        private int currentFrameIndex;

        /// <summary>
        /// Gets or sets the flag that indicates that the spinner should be spinning
        /// </summary>
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

        /// <summary>
        /// Creates a new spinner
        /// </summary>
        public Spinner()
        {
            currentFrameIndex = 0;
            this.AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this);
            this.RemovedFromVisualTree.SubscribeForLifetime(OnRemovedFromVisualTree, this);
            this.CanFocus = false;
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
            spinTimerHandle.Dispose();
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
