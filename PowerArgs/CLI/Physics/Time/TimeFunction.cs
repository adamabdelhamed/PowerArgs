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
        string Id { get; set; }

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
    }

    /// <summary>
    /// A base class to use for general purpose time functions that implements all but the
    /// functional elements of the time function interface
    /// 
    /// </summary>
    public abstract class TimeFunction : ITimeFunction
    {
        public string Id { get; set; }
 

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
 

        public HashSet<string> Tags { get; set; } = new HashSet<string>();

        public void AddTags(IEnumerable<string> tags)
        {
            foreach(var tag in tags)
            {
                Tags.Add(tag);
            }
        }

        public bool HasSimpleTag(string tag) => Tags.Where(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)).Any();
        public bool HasValueTag(string tag) => Tags.Where(t => t.StartsWith(tag + ":", StringComparison.OrdinalIgnoreCase)).Any();

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