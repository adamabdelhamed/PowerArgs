using PowerArgs.Cli.Physics;
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
        public EasingFunction EasingFunction { get; set; } = Animator.EaseInOut;

        /// <summary>
        /// If true then the animation will automatically reverse itself when done
        /// </summary>
        public bool AutoReverse { get; set; }

        /// <summary>
        /// When specified, the animation will loop until this lifetime completes
        /// </summary>
        public ILifetimeManager Loop { get; set; }

        /// <summary>
        /// The provider to use for delaying between animation frames
        /// </summary>
        public IDelayProvider DelayProvider { get; set; }  

        /// <summary>
        /// If auto reverse is enabled, this is the pause, in milliseconds, after the forward animation
        /// finishes, to wait before reversing
        /// </summary>
        public float AutoReverseDelay { get; set; } = 0;

        /// <summary>
        /// A callback that indicates that we should end the animation early
        /// </summary>
        public Func<bool> IsCancelled { get; set; }

        /// <summary>
        /// A callback that is called before a value is set. The parameter is the percentage done.
        /// </summary>
        public Action<float> OnSet { get; set; }

        /// <summary>
        /// A callback that indicates that the animation should pause
        /// </summary>
        public Func<bool> IsPaused { get; set; }

        internal abstract void Set(float value);

        internal Action<string> Debug { get; set; }

        /// <summary>
        /// A callback that lets you know if the animation is running in reverse (true) or forward (false).
        /// Forward is the default so this will not fire with false unless the animation loops.
        /// </summary>
        public Action<bool> OnReversedChanged { get; set; }

        public async Task<bool> HandlePause()
        {
            var ret = false;
            while (IsPaused != null && IsPaused.Invoke())
            {
                ret = true;
                await Task.Yield();
            }
            return ret;
        }

        public async Task YieldAsync()
        {
            if (await HandlePause() == false)
            {
                await Task.Yield();
            }
        }

        public async Task DelayAsync(TimeSpan ts)
        {
            if (await HandlePause() == false)
            {
                await DelayProvider.DelayAsync(ts);
            }
        }
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
        internal override void Set(float value) => Setter(Geometry.Round(value));
    }


    public delegate float EasingFunction(float f);

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
        public static float EaseOut(float percentage) => (float)Math.Pow(percentage, 1.0f / 4);

        /// <summary>
        /// An easing function that starts fast and decellerates as time moves on
        /// </summary>
        /// <param name="percentage">the linear percentage</param>
        /// <returns>the eased percentage</returns>

        public static float EaseOutSoft(float percentage) => (float)Math.Pow(percentage, 1.0f / 2);

        /// <summary>
        /// An easing function that starts and ends slow, but peaks at the midpoint
        /// </summary>
        /// <param name="percentage">the linear percentage</param>
        /// <returns>the eased percentage</returns>
        public static float EaseInOut(float percentage) => percentage < .5 ? 4 * percentage * percentage * percentage : (percentage - 1) * (2 * percentage - 2) * (2 * percentage - 2) + 1;

        private const int TargetFramesPerSecond = 20;
        
        /// <summary>
        /// Performs the animation specified in the options
        /// </summary>
        /// <param name="options">animation options</param>
        /// <returns>an async task</returns>
        public static async Task AnimateAsync(AnimatorOptions options)
        {
            if(options.DelayProvider == null && Time.CurrentTime != null)
            {
                options.DelayProvider = Time.CurrentTime;
            }
            else if(options.DelayProvider == null)
            {
                options.DelayProvider = new WallClockDelayProvider();
            }

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
                            await options.DelayAsync(TimeSpan.FromMilliseconds(options.AutoReverseDelay));
                        }

                        var temp = options.From;
                        options.From = options.To;
                        options.To = temp;
                        options.OnReversedChanged?.Invoke(true);
                        await AnimateAsyncInternal(options);

                        if (options.AutoReverseDelay > 0)
                        {
                            await options.DelayAsync(TimeSpan.FromMilliseconds(options.AutoReverseDelay));
                        }

                        options.From = originalFrom;
                        options.To = originalTo;
                        options.OnReversedChanged?.Invoke(false);
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

            var numberOfFrames = (float)(Geometry.Round(animationTime.TotalSeconds * TargetFramesPerSecond));
            numberOfFrames = Math.Max(numberOfFrames, 2);
#if DEBUG
            options.Debug?.Invoke($"Frames: {numberOfFrames}");
#endif
            var timeBetweenFrames = TimeSpan.FromMilliseconds(Geometry.Round(animationTime.TotalMilliseconds / numberOfFrames));
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
                options.OnSet?.Invoke(percentageDone);
                options.Set(newValue);
#if DEBUG
                options.Debug?.Invoke($"Set value to {newValue} at percentage {percentageDone}");
#endif
                var delayTime = options.DelayProvider is WallClockDelayProvider ? TimeSpan.FromMilliseconds(Math.Max(0, scheduledTimeAfterThisFrame.TotalMilliseconds - workSw.Elapsed.TotalMilliseconds)) : timeBetweenFrames;
#if DEBUG
                options.Debug?.Invoke($"Delayed for {delayTime.TotalMilliseconds} ms at percentage {percentageDone}");
#endif

                if(options.IsCancelled != null && options.IsCancelled())
                {
                    return;
                }
                if (delayTime == TimeSpan.Zero)
                {
                    await options.YieldAsync();
                }
                else
                {
                    await options.DelayAsync(delayTime);
                }
            }
        }
    }
}
