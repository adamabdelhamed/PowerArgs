using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Preview
{
    [ArgPipelineActionStage("Filter")]
    internal class Filter : InProcessPipelineStage
    {
        public Filter(string[] commandLine) : base(commandLine) { }

        protected override void OnObjectReceived(object o)
        {
            CommandLineArgumentsDefinition def = new CommandLineArgumentsDefinition(typeof(FilterAction));
            def.FindMatchingArgument("FilterTarget").RevivedValueOverride = o;
            Args.InvokeMain(def, CmdLineArgs.ToArray());
        }
    }

    /// <summary>
    /// Represents the filter operations supported by the $filter pipeline action stage
    /// </summary>
    public enum FilterOperators
    {
        /// <summary>
        /// Equals
        /// </summary>
        [ArgShortcut("==")]
        Equals,
        /// <summary>
        /// Greater than
        /// </summary>
        [ArgShortcut(">")]
        GreaterThan,
        /// <summary>
        /// Less than
        /// </summary>
        [ArgShortcut("<")]
        LessThan,
        /// <summary>
        /// Greater than or equal to
        /// </summary>
        [ArgShortcut(">=")]
        GreaterThanOrEqualTo,
        /// <summary>
        /// Less than or equal to
        /// </summary>
        [ArgShortcut("<=")]
        LessThanOrEqualTo,
        /// <summary>
        /// Not equals
        /// </summary>
        [ArgShortcut("!=")]
        NotEquals,
        /// <summary>
        /// Contains
        /// </summary>
        Contains,
        /// <summary>
        /// Not contains
        /// </summary>
        NotContains,
    }

    internal class FilterAction
    {
        [ArgPipelineTarget(PipelineOnly=true), ArgRequired]
        public object FilterTarget { get; set; }

        [ArgPosition(0), ArgRequired]
        public string PropertyName { get; set; }

        [ArgPosition(1), ArgDefaultValue("==")]
        public FilterOperators Operator { get; set; }

        [ArgPosition(2), ArgRequired]
        public string Value { get; set; }

        public void Main()
        {
            object eval;

            if (PropertyName == "$item")
            {
                eval = FilterTarget;
            }
            else
            {
                var evalProp = FilterTarget.GetType().GetProperty(PropertyName);
                if (evalProp == null) return;
                if (evalProp.GetGetMethod() == null) return;

                eval = evalProp.GetValue(FilterTarget, null);
                if (eval == null) return;
            }

            if(eval is string == false && (Operator == FilterOperators.Contains || Operator == FilterOperators.NotContains))
            {
                throw new ArgException("The Contains and NotContains operators can only be applied to properties of type string");
            }

            object comparison = Value;

            try
            {
                if (eval.GetType() != typeof(string))
                {
                    if(ArgRevivers.CanRevive(eval.GetType()))
                    {
                        comparison = ArgRevivers.Revive(eval.GetType(), "", comparison+"");
                    }
                }
            }
            catch(Exception ex)
            {
                PowerLogger.LogLine("Unable to convert a string to:" + eval.GetType().FullName);
                return;
            }

            if (FilterLogic.FilterAcceptsObject(eval, Operator, comparison))
            {
                ArgPipeline.Push(FilterTarget);
            }
        }
    }

    /// <summary>
    /// Only marked public for testing.  Please do not use.
    /// </summary>
    public static class FilterLogic
    {
        /// <summary>
        /// Only marked public for testing.  Please do not use.
        /// </summary>
        /// <param name="firstOperand">Only marked public for testing.  Please do not use.</param>
        /// <param name="filterOperator">Only marked public for testing.  Please do not use.</param>
        /// <param name="secondOperand">Only marked public for testing.  Please do not use.</param>
        /// <returns>Only marked public for testing.  Please do not use.</returns>
        public static bool FilterAcceptsObject(object firstOperand, FilterOperators filterOperator, object secondOperand)
        {
            if (filterOperator == FilterOperators.Equals && firstOperand.Equals(secondOperand))
            {
                return true;
            }
            if (filterOperator == FilterOperators.NotEquals && !firstOperand.Equals(secondOperand))
            {
                return true;
            }
            else if (filterOperator == FilterOperators.GreaterThan && firstOperand.GetType().GetInterfaces().Contains(typeof(IComparable)) && ((IComparable)firstOperand).CompareTo(secondOperand) > 0)
            {
                return true;
            }
            else if (filterOperator == FilterOperators.GreaterThanOrEqualTo && firstOperand.GetType().GetInterfaces().Contains(typeof(IComparable)) && ((IComparable)firstOperand).CompareTo(secondOperand) >= 0)
            {
                return true;
            }
            else if (filterOperator == FilterOperators.LessThan && firstOperand.GetType().GetInterfaces().Contains(typeof(IComparable)) && ((IComparable)firstOperand).CompareTo(secondOperand) < 0)
            {
                return true;
            }
            else if (filterOperator == FilterOperators.LessThanOrEqualTo && firstOperand.GetType().GetInterfaces().Contains(typeof(IComparable)) && ((IComparable)firstOperand).CompareTo(secondOperand) <= 0)
            {
                return true;
            }
            else if (filterOperator == FilterOperators.Contains)
            {
                if (firstOperand is string && ((string)firstOperand).Contains((string)secondOperand))
                {
                    return true;
                }
            }
            else if (filterOperator == FilterOperators.NotContains)
            {
                if (firstOperand is string && ((string)firstOperand).Contains((string)secondOperand) == false)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
