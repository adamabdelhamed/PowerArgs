using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public abstract class BestOfStrategy : IBotStrategy
    {
        private IApplicableStrategy current;
        public IApplicableStrategy CurrentStrategy
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                if (current != null)
                {
                    current.Me = Me;
                }
            }
        }

        private Character me;
        public Character Me
        {
            get => me;
            set
            {
                me = value;
                if (current != null && current.Me != me)
                {
                    current.Me = me;
                }
            }
        }
        public RateGovernor EvalGovernor => CurrentStrategy != null ? CurrentStrategy.EvalGovernor : new RateGovernor(TimeSpan.FromSeconds(.05));

        public List<Func<IApplicableStrategy>> Children { get; private set; } = new List<Func<IApplicableStrategy>>();
        protected StrategyEval latestEval;
        protected int latestEvalIndex;

        protected List<IApplicableStrategy> FreshChildren()
        {
            var ret = new List<IApplicableStrategy>();
            foreach (var child in Children)
            {
                var c = child();
                c.Me = me;
                ret.Add(c);
            }
            return ret;
        }

        protected StrategyEval GetBestChildStrategy(out int index)
        {
            StrategyEval maxEval = null;
            int maxIndex = -1;
            var freshChildren = FreshChildren();
            for (var i = 0; i < freshChildren.Count; i++)
            {
                var c = freshChildren[i];
                var eval = c.EvaluateApplicability();
                if (eval.Applicability > 0)
                {
                    if (maxEval == null || eval.Applicability > maxEval.Applicability)
                    {
                        maxEval = eval;
                        maxIndex = i;
                    }
                }
            }
            index = maxIndex;
            return maxEval;
        }

        public void Work()
        {
            if (CurrentStrategy == null)
            {
                var maxEval = GetBestChildStrategy(out int maxIndex);

                if (maxEval != null)
                {
                    latestEval = maxEval;
                    latestEvalIndex = maxIndex;
                    CurrentStrategy = (IApplicableStrategy)latestEval.Strategy;
                }
            }
            else
            {
                StrategyEval maxEval = null;
                int maxIndex = -1;
                var freshChildren = FreshChildren();
                for (var i = 0; i < Children.Count; i++)
                {
                    if (i == latestEvalIndex) continue;

                    var c = freshChildren[i];
                    var eval = c.EvaluateApplicability();
                    if (eval.Applicability > 0)
                    {
                        if (maxEval == null || eval.Applicability > maxEval.Applicability)
                        {
                            maxEval = eval;
                            maxIndex = i;
                        }
                    }
                }

                if (maxEval != null && maxEval.Applicability == 1)
                {
                    CurrentStrategy.OnInterrupted();
                    latestEval = maxEval;
                    latestEvalIndex = maxIndex;
                    CurrentStrategy = (IApplicableStrategy)latestEval.Strategy;
                }
            }

            CurrentStrategy?.Work();
        }
    }
}
