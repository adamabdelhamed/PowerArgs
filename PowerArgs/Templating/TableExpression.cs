using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// A document expression that can be used to render a table that is specifically formatted to display
    /// in a console window.
    /// </summary>
    public class TableExpression : IDocumentExpression
    {
        /// <summary>
        /// The token representing the expression to evaluate.  The expression is expected to resolve to
        /// an IEnumerable.
        /// </summary>
        public DocumentToken EvalToken { get; private set; }

        /// <summary>
        /// The tokens that represent the columns to display.  Each value is expected to be a property name.  Optionally, you can
        /// override the display name by appending '>NewDisplayName' to the value.  For example, if there was a property called 'TheName'
        /// and you wanted it to display as 'Name' then the value of the column token would be 'TheName>Name'.
        /// </summary>
        public List<DocumentToken> Columns { get; private set; }

        /// <summary>
        /// PowerArgs specific property - Set to false if using outside of Powerargs - If true then properties that are enums 
        /// will have their values inserted into the table.
        /// </summary>
        public bool ShowPossibleValuesForArguments { get; set; }

        /// <summary>
        /// PowerArgs specific property - Set to false if using outside of Powerargs - If true then the properties that have a default 
        /// value will have that inserted into the table
        /// </summary>
        public bool ShowDefaultValuesForArguments { get; set; }

        /// <summary>
        /// Creates a new table expression given a collection evaluation expression and a list of column tokens
        /// </summary>
        /// <param name="evalToken">A token containing an expression that should evaluate to an IEnumerable</param>
        /// <param name="columns">A list of tokens containing the names of columns to display in the table</param>
        public TableExpression(DocumentToken evalToken, List<DocumentToken> columns)
        {
            this.EvalToken = evalToken;
            this.Columns = columns;
            this.ShowDefaultValuesForArguments = true;
            this.ShowPossibleValuesForArguments = true;
        }

        /// <summary>
        /// Renders the table given a data context
        /// </summary>
        /// <param name="context">the data context</param>
        /// <returns>the console friendly table, as a ConsoleString</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
        {
            var eval = context.EvaluateExpression(this.EvalToken.Value);

            if(eval == null)
            {
                throw new DocumentRenderException("NullReference for '" + this.EvalToken.Value + "'", this.EvalToken);
            }
            else if(eval is IEnumerable == false)
            {
                throw new DocumentRenderException("'" + this.EvalToken.Value + "' is not enumerable", this.EvalToken);
            }

            IEnumerable collection = (IEnumerable)eval;

            List<ConsoleString> headers = new List<ConsoleString>();
            List<List<ConsoleString>> rows = new List<List<ConsoleString>>();

            foreach (var col in Columns)
            {
                if (col.Value.Contains(">"))
                {
                    var newColName = col.Value.Split('>')[1];
                    headers.Add(new ConsoleString(newColName, ConsoleColor.Yellow));
                }
                else
                {
                    headers.Add(new ConsoleString(col.Value, ConsoleColor.Yellow));
                }
            }

            foreach(var element in collection)
            {
                var row = new List<ConsoleString>();
                foreach (var col in Columns)
                {
                    string propName;
                    if (col.Value.Contains(">"))
                    {
                        propName = col.Value.Split('>')[0];
                    }
                    else
                    {
                        propName = col.Value;
                    }

                    var propToGet = element.GetType().GetProperty(propName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (propToGet == null) throw new DocumentRenderException("'" + col.Value + "' is not a valid property for type '" + element.GetType().FullName + "'", col);
                    var value = propToGet.GetValue(element, null);

                    ConsoleString valueString;

                    if(value != null)
                    {
                        valueString = new ConsoleString(value.ToString());
                        if (ShowDefaultValuesForArguments && element is CommandLineArgument && propToGet.Name == "Description" && ((CommandLineArgument)element).DefaultValue != null)
                        {
                            valueString+= new ConsoleString(" [Default='" + ((CommandLineArgument)element).DefaultValue.ToString() + "'] ", ConsoleColor.DarkGreen);
                        }
                    }
                    else
                    {
                        valueString = ConsoleString.Empty;
                    }
                    row.Add(valueString);
                }
                rows.Add(row);

                if(ShowPossibleValuesForArguments && element is CommandLineArgument && ((CommandLineArgument)element).ArgumentType.IsEnum)
                {
                    foreach (var val in ((CommandLineArgument)element).EnumValuesAndDescriptions)
                    {
                        List<ConsoleString> possibilitiesRow = new List<ConsoleString>();
                        for (int i = 0; i < Columns.Count - 1; i++)
                        {
                            possibilitiesRow.Add(ConsoleString.Empty);
                        }
                        possibilitiesRow.Add(new ConsoleString(val, ConsoleColor.DarkGreen));
                        rows.Add(possibilitiesRow);
                    }   
                }
            }

            var tableText = FormatAsTable(headers, rows);

            return tableText;
        }

        /// <summary>
        /// Formats the given column headers and rows as a table.
        /// </summary>
        /// <param name="columns">The column headers</param>
        /// <param name="rows">The row data</param>
        /// <param name="rowPrefix">A string to prepend to each row.  This can be used to indent a table.</param>
        /// <returns>The rendered table as a ConsoleString</returns>
        public static ConsoleString FormatAsTable(List<ConsoleString> columns, List<List<ConsoleString>> rows, string rowPrefix = "")
        {
            if (rows.Count == 0) return new ConsoleString();

            Dictionary<int, int> maximums = new Dictionary<int, int>();

            for (int i = 0; i < columns.Count; i++) maximums.Add(i, columns[i].Length);
            for (int i = 0; i < columns.Count; i++)
            {
                foreach (var row in rows)
                {
                    maximums[i] = Math.Max(maximums[i], row[i].Length);
                }
            }

            ConsoleString ret = new ConsoleString();
            int buffer = 3;

            ret += rowPrefix;
            for (int i = 0; i < columns.Count; i++)
            {
                var val = columns[i];
                while (val.Length < maximums[i] + buffer) val += " ";
                ret += val;
            }

            ret += "\n";

            foreach (var row in rows)
            {
                ret += rowPrefix;
                for (int i = 0; i < columns.Count; i++)
                {
                    var val = row[i];
                    while (val.Length < maximums[i] + buffer) val += " ";

                    ret += val;
                }
                ret += "\n";
            }

            return ret;
        }
    }

    /// <summary>
    /// A provider that can create a table expression given a replacement token and parameters.
    /// </summary>
    public class TableExpressionProvider : IDocumentExpressionProvider
    {
        /// <summary>
        /// Creates a table expression from the given document info.
        /// </summary>
        /// <param name="replacementKeyToken">The token that should contain a value of 'table'</param>
        /// <param name="parameters">Replacement parameters that should be column names and optional properties</param>
        /// <param name="body">Should be empty.  Table expressions don't support bodies</param>
        /// <returns>The created document expression</returns>
        public IDocumentExpression CreateExpression(DocumentToken replacementKeyToken, List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new DocumentRenderException("table tags can't have a body", replacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableExpressionToken;

            if (reader.TryAdvance(out variableExpressionToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing collection expression after table tag", replacementKeyToken);
            }

            List<DocumentToken> columns = new List<DocumentToken>();

            bool showDefaults = true;
            bool showPossibilities = true;

            while(reader.CanAdvance(skipWhitespace: true))
            {
                var nextToken = reader.Advance(skipWhitespace: true);

                if (nextToken.Value == "-HideDefaults")
                {
                    showDefaults = false;
                }
                else if(nextToken.Value == "-HideEnumValues")
                {
                    showPossibilities = false;
                }
                else
                {
                    columns.Add(nextToken);
                }
            }

            if (columns.Count == 0)
            {
                throw new DocumentRenderException("table elements need to have at least one column parameter", replacementKeyToken);
            }

            return new TableExpression(variableExpressionToken, columns) { ShowDefaultValuesForArguments = showDefaults, ShowPossibleValuesForArguments = showPossibilities };
        }
    }
}
