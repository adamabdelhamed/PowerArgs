using System;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// An interface for a time function that can be plugged into a time simulation
    /// </summary>
    public interface ITimeFunction
    {
        /// <summary>
        /// An event that will be fired when this function is added to a time model
        /// </summary>
        Event Added { get; }

        /// <summary>
        /// Used internally for bookkeeping. Implementors should just new up one of these upon
        /// construction
        /// </summary>
        TimeFunctionInternalState InternalState { get; }

        /// <summary>
        /// Gets the lifetime of this time function. The end point of the lifetime
        /// will be when this function is removed from a time model.
        /// </summary>
        Lifetime Lifetime { get; }

        /// <summary>
        /// Gets the rate governor that determines how frequently this function should be evaluated. 
        /// The actual evaulation interval will be either the governor value or the time model's increment value, 
        /// whichever is largest.
        /// </summary>
        RateGovernor Governor { get; }

        /// <summary>
        /// An initialization function that will be called when the function is added to the model
        /// </summary>
        void Initialize();

        /// <summary>
        /// The method that will be called by the time model when it is time for this function to
        /// be evaluated.
        /// </summary>
        void Evaluate();
    }

    /// <summary>
    /// A base class to use for general purpose time functions that implements all but the
    /// functional elements of the time function interface
    /// 
    /// </summary>
    public abstract class TimeFunction : ITimeFunction
    {
        private class ActionTimeFunction : TimeFunction
        {
            public Action Init { get; set; }
            public Action Eval { get; set; }
            public override void Initialize() { Init?.Invoke(); }
            public override void Evaluate() { Eval?.Invoke(); }
        }

        /// <summary>
        /// An event that will be fired when this function is added to a time model
        /// </summary>
        public Event Added { get; private set; } = new Event();

        /// <summary>
        /// Internal state
        /// </summary>
        public TimeFunctionInternalState InternalState { get; protected set; } = new TimeFunctionInternalState();

        /// <summary>
        /// Gets the lifetime of this time function. The end point of the lifetime
        /// will be when this function is removed from a time model.
        /// </summary>
        public Lifetime Lifetime { get; private set; } = new Lifetime();

        /// <summary>
        /// Gets the rate governor that determines how frequently this function should be evaluated. 
        /// The actual evaulation interval will be either the governor value or the time model's increment value, 
        /// whichever is largest.
        /// </summary>
        public RateGovernor Governor { get; protected set; } = new RateGovernor(TimeSpan.Zero);

        /// <summary>
        /// An initialization function that will be called when the function is added to the model
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Gets the rate governor that determines how frequently this function should be evaluated. 
        /// The actual evaulation interval will be either the governor value or the time model's increment value, 
        /// whichever is largest.
        /// </summary>
        public abstract void Evaluate();

        /// <summary>
        /// Creates a time function given action code to run
        /// </summary>
        /// <param name="eval">The evaluation action to run</param>
        /// <param name="init">An optional initialization function</param>
        /// <param name="rate">The governor rate for the function</param>
        /// <returns>A time function that can be added into a time model</returns>
        public static ITimeFunction Create(Action eval, Action init = null, TimeSpan? rate = null)
        {
            var ret = new ActionTimeFunction() { Eval = eval, Init = init };
            ret.Governor = new RateGovernor(rate.HasValue ? rate.Value : ret.Governor.Rate);
            return ret;
        }

        /// <summary>
        /// Creates a time function that will not initialzie / evaluate untl after the returned function has been added to Time
        /// for the delay period
        /// </summary>
        /// <param name="delay">The amount of time to wait before creating the function</param>
        /// <param name="eval">the evaluate method</param>
        /// <param name="init">the initialize method</param>
        /// <param name="rate">the rate at which the evaluate function runs</param>
        /// <returns>A delayed time function</returns>
        public static ITimeFunction CreateDelayed(TimeSpan delay, Action eval,  Action init = null, TimeSpan? rate = null)
        {
            ITimeFunction ret = null;
            ret = Create(() =>
            {
                if(ret.CalculateAge() >= delay)
                {
                    ret.Lifetime.Dispose();
                    SpaceTime.CurrentSpaceTime.Add(Create(eval, init, rate));
                }

            });
            return ret;
        }
    }

    /// <summary>
    /// Extension methods that target the ITimeFunction interface
    /// </summary>
    public static class ITimeFunctionExtensions
    {
        /// <summary>
        /// Gets the age of the given function defined as the amount of simulation time that the function has been a part of the model.
        /// </summary>
        /// <param name="function">the function to target</param>
        /// <returns>The age, as a time span</returns>
        public static TimeSpan CalculateAge(this ITimeFunction function) => Time.CurrentTime.Now - function.InternalState.AddedTime;

        /// <summary>
        /// Determines if the given function is currently attached to a time simulation
        /// </summary>
        /// <param name="function">the function to target</param>
        /// <returns>true if attached to a time model, false otherwise</returns>
        public static bool IsAttached(this ITimeFunction function) => function.InternalState.AttachedTime != null;
    }

    /// <summary>
    /// A bookkeeping class that is used internally
    /// </summary>
    public class TimeFunctionInternalState
    {
        internal Time AttachedTime { get; set; }
        internal TimeSpan AddedTime { get; set; }
    }
}