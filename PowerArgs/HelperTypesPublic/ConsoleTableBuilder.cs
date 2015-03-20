using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// A class that makes it easy to build strings that look and feel like tables when displayed in a console
    /// </summary>
    public class ConsoleTableBuilder
    {
        private static ConsoleString Space = new ConsoleString(" ");

        private class ConsoleTableBuilderContext
        {
            public ConsoleString RowPrefix { get; set; }
            public bool LastColumnAutoExpands { get; set; }
            public List<ConsoleString> ColumnHeaders { get; set; }
            public List<List<ConsoleString>> Rows { get; set; }
            public List<ColumnOverflowBehavior> OverflowBehaviors { get; set; }
            public Dictionary<int, int> ColumnWidths { get; set; }
            public ConsoleString TableOutput { get; set; }
            public int Gutter { get; set; }
            public List<SmartWrapInterrupt> SmartWrapInterrupts { get; set; }
        }

        private class SmartWrapInterrupt : IComparable<SmartWrapInterrupt>
        {
            public int Row { get; set; }
            public int Column { get; set; }
            public ConsoleString Value { get; set; }
            public SmartWrapOverflowBehavior SmartWrap { get; set; }

            public int CompareTo(SmartWrapInterrupt other)
            {
                if (this.Row != other.Row)
                {
                    return this.Row.CompareTo(other.Row);
                }
                else
                {
                    return this.Column.CompareTo(other.Column);
                }
            }
        }

        /// <summary>
        /// Formats the given data as a string that looks and feels like a table when displayed in a console
        /// </summary>
        /// <param name="columnHeaders">The headers for the table</param>
        /// <param name="rows">The cell data for the table</param>
        /// <param name="rowPrefix">A prefix, usually an indentation, to append before each row, including the headers</param>
        /// <param name="columnOverflowBehaviors">Optionally provide hints as to how overflow should be handled.  By default, the longest value in a column determines the column width.  You can choose to truncate or to do a smart wrap.</param>
        /// <param name="gutter">How many empty spaces to place between columns</param>
        /// <returns>The table, as a console string</returns>
        public ConsoleString FormatAsTable(List<ConsoleString> columnHeaders, List<List<ConsoleString>> rows, string rowPrefix = "", List<ColumnOverflowBehavior> columnOverflowBehaviors = null, int gutter = 3)
        {
            ConsoleTableBuilderContext context = new ConsoleTableBuilderContext();
            context.Gutter = gutter;
            context.ColumnHeaders = columnHeaders;
            context.Rows = rows ?? new List<List<ConsoleString>>();
            context.OverflowBehaviors = columnOverflowBehaviors ?? CreateDefaultOverflowBehavior(context.ColumnHeaders.Count);
            context.TableOutput = new ConsoleString();
            context.SmartWrapInterrupts = new List<SmartWrapInterrupt>();
            context.RowPrefix = new ConsoleString(rowPrefix);

            ValidateInputs(context);
            DetermineColumnWidths(context);
            AddColumnHeadersToTable(context);
            AddCellsToTable(context);

            return context.TableOutput;
        }

        /// <summary>
        /// Formats the given collection as a string that looks and feels like a table when displayed in a console.
        /// </summary>
        /// <param name="objects">The objects to format</param>
        /// <param name="format">A space delimited set of properties to use as column headers.  
        /// You can change the display string for a particular property by using the format 'PropertyName>DisplayName' where PropertyName is a property name and DisplayName is the text to display.  If you omit this parameter then the first object in the collection will be inspected and it's public properties will be used as columns.</param>
        /// <returns></returns>
        public ConsoleString FormatAsTable(IEnumerable objects, string format = null)
        {
            var prototype = GetPrototype(objects);
            if(prototype == null)
            {
                return ConsoleString.Empty;
            }

            format = format ?? InferFormat(prototype);
            var documentTemplate = "{{ table objects "+format+" !}}";
            DocumentRenderer renderer = new DocumentRenderer();
            var document = renderer.Render(documentTemplate, new { objects = objects });
            return document;
        }


        private object GetPrototype(IEnumerable objects)
        {
            object prototype = null;
            foreach (var o in objects)
            {
                prototype = o;
                break;
            }

            return prototype;
        }

        private string InferFormat(object prototype)
        {
            var ret = string.Join(" ", prototype.GetType().GetProperties().Select(p => p.Name));
            ret += "+";
            return ret;
        }

        private List<ColumnOverflowBehavior> CreateDefaultOverflowBehavior(int numColumns)
        {
            var ret = new List<ColumnOverflowBehavior>();
            for (int i = 0; i < numColumns; i++)
            {
                ret.Add(new GrowUnboundedOverflowBehavior());
            }
            return ret;
        }

        private void DetermineColumnWidths(ConsoleTableBuilderContext context)
        {
            Dictionary<int, int> ret = new Dictionary<int, int>();

            for (int i = 0; i < context.ColumnHeaders.Count; i++) ret.Add(i, context.ColumnHeaders[i].Length);
            for (int i = 0; i < context.ColumnHeaders.Count; i++)
            {
                if (context.OverflowBehaviors[i] is TruncateOverflowBehavior)
                {
                    ret[i] = (context.OverflowBehaviors[i] as TruncateOverflowBehavior).ColumnWidth + (context.OverflowBehaviors[i] as TruncateOverflowBehavior).TruncationText.Length;
                }
                else if (context.OverflowBehaviors[i] is SmartWrapOverflowBehavior)
                {
                    if ((context.OverflowBehaviors[i] as SmartWrapOverflowBehavior).DefineMaxWidthBasedOnConsoleWidth)
                    {
                        if (i != context.ColumnHeaders.Count - 1)
                        {
                            throw new InvalidOperationException("DefineMaxWidthBasedOnConsoleWidth can only be set to true for the last column in a table");
                        }
                        context.LastColumnAutoExpands = true;
                        var left = 0;

                        for (int j = 0; j < i; j++)
                        {
                            left += ret[j] + context.Gutter;
                        }
                        (context.OverflowBehaviors[i] as SmartWrapOverflowBehavior).MaxWidthBeforeWrapping = ConsoleProvider.Current.BufferWidth - left - context.Gutter; // The Gutter is so newlines don't get double rendered on the console
                    }

                    ret[i] = (context.OverflowBehaviors[i] as SmartWrapOverflowBehavior).MaxWidthBeforeWrapping;
                }
                else if (context.OverflowBehaviors[i] is GrowUnboundedOverflowBehavior)
                {
                    foreach (var row in context.Rows)
                    {
                        ret[i] = Math.Max(ret[i], row[i].Length);
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported overflow behavior: '" + context.OverflowBehaviors[i].GetType().FullName + "'");
                }
            }

            context.ColumnWidths = ret;
        }

        private void AddCellsToTable(ConsoleTableBuilderContext context)
        {
            for (int rowIndex = 0; rowIndex < context.Rows.Count; rowIndex++)
            {
                context.TableOutput += context.RowPrefix;

                for (int colIndex = 0; colIndex < context.ColumnHeaders.Count; colIndex++)
                {
                    AddCellToTable(context, rowIndex, colIndex);
                }

                context.TableOutput += "\n";
                ProcessPendingSmartWrapInterrupts(context);
            }
        }

        private void AddCellToTable(ConsoleTableBuilderContext context, int rowIndex, int colIndex)
        {
            var val = context.Rows[rowIndex][colIndex];

            if (context.OverflowBehaviors[colIndex] is TruncateOverflowBehavior)
            {
                val = TruncateIfNeeded((context.OverflowBehaviors[colIndex] as TruncateOverflowBehavior), val);
                while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
            }
            else if (context.OverflowBehaviors[colIndex] is SmartWrapOverflowBehavior)
            {
                var segments = FormatAsWrappedSegments((context.OverflowBehaviors[colIndex] as SmartWrapOverflowBehavior), val);
                val = segments[0];

                if ((context.OverflowBehaviors[colIndex] as SmartWrapOverflowBehavior).DefineMaxWidthBasedOnConsoleWidth == false)
                {
                    while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
                }

                for (int segIndex = 1; segIndex < segments.Count; segIndex++)
                {
                    var interrupt = new SmartWrapInterrupt()
                    {
                        Column = colIndex,
                        Row = rowIndex + (segIndex - 1),
                        Value = segments[segIndex],
                        SmartWrap = (context.OverflowBehaviors[colIndex] as SmartWrapOverflowBehavior)
                    };
                    context.SmartWrapInterrupts.Add(interrupt);
                }

            }
            else if (context.OverflowBehaviors[colIndex] is GrowUnboundedOverflowBehavior)
            {
                while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
            }
            else
            {
                throw new NotSupportedException("Unsupported overflow behavior: '" + context.OverflowBehaviors[colIndex].GetType().FullName + "'");
            }

            context.TableOutput += val;
        }

        private void ProcessPendingSmartWrapInterrupts(ConsoleTableBuilderContext context)
        {
            while (context.SmartWrapInterrupts.Count > 0)
            {
                context.SmartWrapInterrupts.Sort();

                context.TableOutput += context.RowPrefix;
                for (int colIndex = 0; colIndex < context.ColumnHeaders.Count; colIndex++)
                {
                    var nextInterruptCol = context.SmartWrapInterrupts.Count > 0 ? context.SmartWrapInterrupts[0].Column : -1;

                    var val = ConsoleString.Empty;
                    if (nextInterruptCol != colIndex)
                    {
                        if (colIndex != context.ColumnHeaders.Count - 1 || !context.LastColumnAutoExpands)
                        {
                            while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
                        }
                    }
                    else
                    {
                        val = context.SmartWrapInterrupts[0].Value;
                        if ((context.OverflowBehaviors[colIndex] as SmartWrapOverflowBehavior).DefineMaxWidthBasedOnConsoleWidth == false)
                        {
                            while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
                        }

                        context.SmartWrapInterrupts.RemoveAt(0);
                    }
                    context.TableOutput += val;
                }

                context.TableOutput += "\n";
            }
        }

        private void AddColumnHeadersToTable(ConsoleTableBuilderContext context)
        {
            context.TableOutput += context.RowPrefix;

            for (int colIndex = 0; colIndex < context.ColumnHeaders.Count; colIndex++)
            {
                var val = context.ColumnHeaders[colIndex];

                if (context.OverflowBehaviors[colIndex] is TruncateOverflowBehavior)
                {
                    val = TruncateIfNeeded((context.OverflowBehaviors[colIndex] as TruncateOverflowBehavior), val);
                    while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
                }
                else if (context.OverflowBehaviors[colIndex] is SmartWrapOverflowBehavior)
                {
                    // column headers don't wrap

                    if ((context.OverflowBehaviors[colIndex] as SmartWrapOverflowBehavior).DefineMaxWidthBasedOnConsoleWidth == false)
                    {
                        while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
                    }

                }
                else if (context.OverflowBehaviors[colIndex] is GrowUnboundedOverflowBehavior)
                {
                    while (val.Length < context.ColumnWidths[colIndex] + context.Gutter) val += Space;
                }
                else
                {
                    throw new NotSupportedException("Unsupported overflow behavior: '" + context.OverflowBehaviors[colIndex].GetType().FullName + "'");
                }

                context.TableOutput += val;
            }

            context.TableOutput += "\n";
        }

        private List<ConsoleString> FormatAsWrappedSegments(SmartWrapOverflowBehavior smartWrap, ConsoleString value)
        {
            List<ConsoleString> ret = new List<ConsoleString>();

            while (value.Length > smartWrap.MaxWidthBeforeWrapping)
            {
                var segment = value.Substring(0, smartWrap.MaxWidthBeforeWrapping);

                int lookBehindLimit = smartWrap.WordBreakLookBehind;
                for (int i = segment.Length - 1; i >= 0 && lookBehindLimit > 0; i--)
                {
                    if (char.IsWhiteSpace(segment[i].Value))
                    {
                        segment = value.Substring(0, i);
                        break;
                    }
                    lookBehindLimit--;
                }

                ret.Add(segment);
                value = value.Substring(segment.Length);
            }

            if (value.Length > 0 || ret.Count == 0)
            {
                ret.Add(value);
            }

            for (int i = 0; i < ret.Count; i++)
            {
                if (ret[i].ToString().Length > 0 && char.IsWhiteSpace(ret[i].ToString()[0]))
                {
                    ret[i] = ret[i].Substring(1);
                }
            }

            return ret;
        }

        private ConsoleString TruncateIfNeeded(TruncateOverflowBehavior truncateBehavior, ConsoleString value)
        {
            if (value.Length > truncateBehavior.MaxWidthBeforeShowingTruncationText)
            {
                value = value.Substring(0, truncateBehavior.MaxWidthBeforeShowingTruncationText) + truncateBehavior.TruncationText;
            }

            return value;
        }

        private void ValidateInputs(ConsoleTableBuilderContext context)
        {
            if (context.OverflowBehaviors.Count != context.ColumnHeaders.Count)
            {
                throw new ArgumentOutOfRangeException("If 'columnOverflowBehaviors' is specified then it must contain the same number of items as 'columns'");
            }
        }
    }

    /// <summary>
    /// An abstract class that lets you describe how to handle variable column widths when formatting a console table.  You should
    /// not derive from this class.  All supported child classes are defined in this assembly.
    /// </summary>
    public abstract class ColumnOverflowBehavior
    {

    }

    /// <summary>
    /// A class that indicates that the target column should be sized based on the longest value in the column, with no upper bound
    /// </summary>
    public class GrowUnboundedOverflowBehavior : ColumnOverflowBehavior
    {

    }

    /// <summary>
    /// A class that indicates that the target column should not exceed a max size, and should be truncated if it does
    /// </summary>
    public class TruncateOverflowBehavior : ColumnOverflowBehavior
    {
        /// <summary>
        /// The truncation indicator, by default '...'
        /// </summary>
        public string TruncationText { get; set; }

        /// <summary>
        /// The width of the column.  Note that your text may be truncated even if it is smaller than this size because the
        /// truncation indicator, by default, takes 3 characters '...'
        /// </summary>
        public int ColumnWidth { get; set; }

        /// <summary>
        /// The max length a value in this column can be without being truncated
        /// </summary>
        public int MaxWidthBeforeShowingTruncationText
        {
            get
            {
                return ColumnWidth - TruncationText.Length;
            }
        }

        /// <summary>
        /// Creates a new truncation overflow behavior instance
        /// </summary>
        public TruncateOverflowBehavior()
        {
            TruncationText = "...";
            ColumnWidth = 10;
        }
    }

    /// <summary>
    /// A class that indicates that the target column should wrap if needed
    /// </summary>
    public class SmartWrapOverflowBehavior : ColumnOverflowBehavior
    {
        /// <summary>
        /// The max length a cell value can be before it needs to wrap
        /// </summary>
        public int MaxWidthBeforeWrapping { get; set; }

        /// <summary>
        /// How far back to look for a whitespace character so that wrapping can be done on a word
        /// </summary>
        public int WordBreakLookBehind { get; set; }

        /// <summary>
        /// If set to true then the target column will have its width dynamically determined based on the width of the current console.  
        /// You can only set this to true for the last column in a table.
        /// </summary>
        public bool DefineMaxWidthBasedOnConsoleWidth { get; set; }

        /// <summary>
        /// Creates a new smart wrap behavior
        /// </summary>
        public SmartWrapOverflowBehavior()
        {
            DefineMaxWidthBasedOnConsoleWidth = true;
            WordBreakLookBehind = 20;
        }
    }
}
