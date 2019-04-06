using System;
using System.Collections.Generic;
using System.Linq;

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
            public Action Eval { get; set; }
            public override void Evaluate() { if (Eval == null) { Lifetime.Dispose(); } else { Eval.Invoke(); } }
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

        public List<string> Tags { get; set; } = new List<string>();

        public bool HasSimpleTag(string tag) => Tags.Where(t => t.ToLower().Equals(tag.ToLower())).Any();
        public bool HasValueTag(string tag) => Tags.Where(t => t.ToLower().StartsWith(tag.ToLower() + ":")).Any();

        public string GetTagValue(string key)
        {
            key = key.ToLower();
            if (TryGetTagValue(key, out string value) == false)
            {
                throw new ArgumentException("There is no value for key: " + key);
            }
            else
            {
                return value;
            }
        }

        public bool TryGetTagValue(string key, out string value)
        {
            key = key.ToLower();
            if (HasValueTag(key))
            {
                var tag = Tags.Where(t => t.ToLower().StartsWith(key + ":")).FirstOrDefault();
                value = ParseTagValue(tag);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private string ParseTagValue(string tag)
        {
            var splitIndex = tag.IndexOf(':');
            if (splitIndex <= 0) throw new ArgumentException("No tag value present for tag: " + tag);

            var val = tag.Substring(splitIndex + 1, tag.Length - (splitIndex + 1));
            return val;
        }


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
        /// <param name="rate">The governor rate for the function</param>
        /// <returns>A time function that can be added into a time model</returns>
        public static ITimeFunction Create(Action eval, TimeSpan? rate = null)
        {
            var ret = new ActionTimeFunction() { Eval = eval };
            ret.Governor = new RateGovernor(rate.HasValue ? rate.Value : ret.Governor.Rate);
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
        public static TimeSpan CalculateAge(this ITimeFunction function) => function.InternalState == null ? TimeSpan.Zero : Time.CurrentTime.Now - function.InternalState.AddedTime;

        /// <summary>
        /// Determines if the given function is currently attached to a time simulation
        /// </summary>
        /// <param name="function">the function to target</param>
        /// <returns>true if attached to a time model, false otherwise</returns>
        public static bool IsAttached(this ITimeFunction function) => function.InternalState != null && function.InternalState.AttachedTime != null;
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