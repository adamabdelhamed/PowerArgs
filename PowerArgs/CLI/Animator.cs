using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Options for doing animations
    /// </summary>
    public abstract class AnimatorOptions
    {
        /// <summary>
        /// The starting value of the animated property
        /// </summary>
        public float From { get; set; }
        /// <summary>
        /// The final value of the animated property
        /// </summary>
        public float To { get; set; }
        /// <summary>
        /// The duration of the animation in milliseconds
        /// </summary>
        public double Duration { get; set; } = 500;

        /// <summary>
        /// The easing function to apply
        /// </summary>
        public Func<float, float> EasingFunction { get; set; } = Animator.EaseInOut;

        /// <summary>
        /// If true then the animation will automatically reverse itself when done
        /// </summary>
        public bool AutoReverse { get; set; }

        /// <summary>
        /// When specified, the animation will loop until this lifetime completes
        /// </summary>
        public ILifetimeManager Loop { get; set; }

        /// <summary>
        /// If auto reverse is enabled, this is the pause, in milliseconds, after the forward animation
        /// finishes, to wait before reversing
        /// </summary>
        public float AutoReverseDelay { get; set; } = 0;

        internal abstract void Set(float value);

        internal Action<string> Debug { get; set; }
    }

    /// <summary>
    /// Animation options to use if you are animating a float
    /// </summary>
    public class FloatAnimatorOptions : AnimatorOptions
    {
        /// <summary>
        /// The function that implements changing the value. It will be called throughout the animation process.
        /// </summary>
        public Action<float> Setter { get; set; }

        internal override void Set(float value) => Setter(value);
    }

    /// <summary>
    /// Animation options to use if you are animating an int
    /// </summary>
    public class RoundedAnimatorOptions : AnimatorOptions
    {
        /// <summary>
        /// The function that implements changing the value. It will be called throughout the animation process.
        /// </summary>
        public Action<int> Setter { get; set; }
        internal override void Set(float value) => Setter((int)Math.Round(value));
    }


    /// <summary>
    /// An animation utility for async code
    /// </summary>
    public class Animator
    {
        /// <summary>
        /// A linear easing function
        /// </summary>
        /// <param name="percentage">the linear percentage</param>
        /// <returns>the linear percentage</returns>
        public static float Linear(float percentage) => percentage;

        /// <summary>
        /// An easing function that starts slow and accellerates as time moves on
        /// </summary>
        /// <param name="percentage">the linear percentage</param>
        /// <returns>the eased percentage</returns>
        public static float EaseIn(float percentage) => (float)Math.Pow(percentage, 5);

        /// <summary>
        /// An easing function that starts fast and decellerates as time moves on
        /// </summary>
        /// <param name="percentage">the linear percentage</param>
        /// <returns>the eased percentage</returns>
        public static float EaseOut(float percentage) => (float)Math.Pow(percentage, 1.0f / 5);

        /// <summary>
        /// An easing function that starts and ends slow, but peaks at the midpoint
        /// </summary>
        /// <param name="percentage">the linear percentage</param>
        /// <returns>the eased percentage</returns>
        public static float EaseInOut(float percentage) => percentage < .5 ? 4 * percentage * percentage * percentage : (percentage - 1) * (2 * percentage - 2) * (2 * percentage - 2) + 1;

        private const int TargetFramesPerSecond = 15;
        
        /// <summary>
        /// Performs the animation specified in the options
        /// </summary>
        /// <param name="options">animation options</param>
        /// <returns>an async task</returns>
        public static async Task AnimateAsync(AnimatorOptions options)
        {
            var originalFrom = options.From;
            var originalTo = options.To;
            try
            {
                var i = 0;
                while (i == 0 || (options.Loop != null && options.Loop.IsExpired == false))
                {
                    i++;
                    await AnimateAsyncInternal(options);

                    if (options.AutoReverse)
                    {
                        if (options.AutoReverseDelay > 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(options.AutoReverseDelay));
                        }

                        var temp = options.From;
                        options.From = options.To;
                        options.To = temp;

                        await AnimateAsyncInternal(options);

                        if (options.Loop != null && options.AutoReverseDelay > 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(options.AutoReverseDelay));
                            options.From = originalFrom;
                            options.To = originalTo;
                        }
                    }
                }
            }
            finally
            {
                options.From = originalFrom;
                options.To = originalTo;
            }
        }

        private static async Task AnimateAsyncInternal(AnimatorOptions options)
        {

            var animationTime = TimeSpan.FromMilliseconds(options.Duration);
            if (animationTime == TimeSpan.Zero)
            {
#if DEBUG
                options.Debug?.Invoke("NoOp animation");
#endif

                options.Set(options.To);
            }

            var numberOfFrames = (float)(Math.Round(animationTime.TotalSeconds * TargetFramesPerSecond));
            numberOfFrames = Math.Max(numberOfFrames, 2);
#if DEBUG
            options.Debug?.Invoke($"Frames: {numberOfFrames}");
#endif
            var timeBetweenFrames = TimeSpan.FromMilliseconds(Math.Round(animationTime.TotalMilliseconds / numberOfFrames));
#if DEBUG
            options.Debug?.Invoke($"Time between frames: {timeBetweenFrames.TotalMilliseconds} ms");
#endif
            var initialValue = options.From;
            options.Set(initialValue);
#if DEBUG
            options.Debug?.Invoke($"InitialValue: {initialValue}");
#endif
            var delta = options.To - initialValue;
#if DEBUG
            options.Debug?.Invoke($"Delta: {delta}");
#endif
            var workSw = Stopwatch.StartNew();
            for(float i = 1; i <= numberOfFrames; i++)
            {
                var percentageDone = i / numberOfFrames;
                if(options.EasingFunction != null)
                {
                    percentageDone = options.EasingFunction(percentageDone);
                }

                var scheduledTimeAfterThisFrame = TimeSpan.FromMilliseconds(timeBetweenFrames.TotalMilliseconds * i);
                var newValue = initialValue + (delta * percentageDone);
                options.Set(newValue);
#if DEBUG
                options.Debug?.Invoke($"Set value to {newValue} at percentage {percentageDone}");
#endif
                var delayTime = TimeSpan.FromMilliseconds(Math.Max(0, scheduledTimeAfterThisFrame.TotalMilliseconds - workSw.Elapsed.TotalMilliseconds));
#if DEBUG
                options.Debug?.Invoke($"Delayed for {delayTime.TotalMilliseconds} ms at percentage {percentageDone}");
#endif
                if (delayTime == TimeSpan.Zero)
                {
                    await Task.Yield();
                }
                else
                {
                    await Task.Delay(delayTime);
                }
            }
        }
    }
}
