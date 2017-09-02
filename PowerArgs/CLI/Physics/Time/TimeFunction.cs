using System;

namespace PowerArgs.Cli.Physics
{
    public interface ITimeFunction
    {
        Event Added { get; }
        TimeFunctionInternalState InternalState { get; }
        Lifetime Lifetime { get; }
        PowerArgs.Cli.Physics.RateGovernor Governor { get; }
        void Initialize();
        void Evaluate();
    }

    public abstract class TimeFunction : ITimeFunction
    {
        private class ActionTimeFunction : TimeFunction
        {
            public Action Init { get; set; }
            public Action Eval { get; set; }
            public override void Initialize() { Init?.Invoke(); }
            public override void Evaluate() { Eval?.Invoke(); }
        }

        public Event Added { get; private set; } = new Event();

        public TimeFunctionInternalState InternalState { get; protected set; } = new TimeFunctionInternalState();
        public Lifetime Lifetime { get; private set; } = new Lifetime();
        public PowerArgs.Cli.Physics.RateGovernor Governor { get; private set; } = new PowerArgs.Cli.Physics.RateGovernor();
        public abstract void Initialize();
        public abstract void Evaluate();

        public static ITimeFunction Create(Action eval, Action init = null, TimeSpan? rate = null)
        {
            var ret = new ActionTimeFunction() { Eval = eval, Init = init };
            ret.Governor.Rate = rate.HasValue ? rate.Value : ret.Governor.Rate;
            return ret;
        }
    }

    public static class ITimeFunctionExtensions
    {
        public static TimeSpan CalculateAge(this ITimeFunction function) => Time.CurrentTime.Now - function.InternalState.AddedTime;
        public static bool IsAttached(this ITimeFunction function) => function.InternalState.AttachedTime != null;
    }

    public class TimeFunctionInternalState
    {
        internal Time AttachedTime { get; set; }
        internal TimeSpan AddedTime { get; set; }
    }

}