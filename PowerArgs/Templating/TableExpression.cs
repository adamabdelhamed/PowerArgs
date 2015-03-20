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

        private int indent;

        /// <summary>
        /// Creates a new table expression given a collection evaluation expression and a list of column tokens
        /// </summary>
        /// <param name="evalToken">A token containing an expression that should evaluate to an IEnumerable</param>
        /// <param name="columns">A list of tokens containing the names of columns to display in the table</param>
        /// <param name="context">Context that is used to determine the indentation of the table within the document</param>
        public TableExpression(DocumentToken evalToken, List<DocumentToken> columns, DocumentExpressionContext context)
        {
            this.EvalToken = evalToken;
            this.Columns = columns;
            this.ShowDefaultValuesForArguments = true;
            this.ShowPossibleValuesForArguments = true;

            this.indent = context.OpenToken.Column - 1;
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
            List<ColumnOverflowBehavior> overflows = new List<ColumnOverflowBehavior>();

            for (int colIndex = 0; colIndex < Columns.Count; colIndex++ )
            {
                var col = Columns[colIndex];
                var colValue = col.Value;

                if (colValue.EndsWith("+"))
                {
                    colValue = colValue.Substring(0, colValue.Length - 1);
                    overflows.Add(new SmartWrapOverflowBehavior());
                    if(colIndex != Columns.Count-1)
                    {
                        throw new DocumentRenderException("The auto expand indicator '+' can only be used on the last column", col);
                    }
                }
                else
                {
                    overflows.Add(new GrowUnboundedOverflowBehavior());
                }

                if (colValue.Contains(">"))
                {
                    var newColName = colValue.Split('>')[1];
                    headers.Add(new ConsoleString(newColName, ConsoleColor.Yellow));
                }
                else
                {
                    headers.Add(new ConsoleString(colValue, ConsoleColor.Yellow));
                }
            }

            foreach(var element in collection)
            {
                if(element is CommandLineArgument && ((CommandLineArgument)element).OmitFromUsage)
                {
                    continue;
                }

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

                    if(propName.EndsWith("+"))
                    {
                        propName = propName.Substring(0, propName.Length - 1);
                    }

                    var propToGet = element.GetType().GetProperty(propName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (propToGet == null) throw new DocumentRenderException("'" + propName + "' is not a valid property for type '" + element.GetType().FullName + "'", col);
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

                if(ShowPossibleValuesForArguments && element is CommandLineArgument && ((CommandLineArgument)element).IsEnum)
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

            string rowPrefix = "";
            for(int i = 0; i < indent; i++)
            {
                rowPrefix += " ";
            }

            ConsoleTableBuilder builder = new ConsoleTableBuilder();
            var tableText = builder.FormatAsTable(headers, rows, rowPrefix: rowPrefix, columnOverflowBehaviors: overflows);

            // remove the prefix from the first row
            tableText = tableText.Substring(indent);
            var tableTextStr = tableText.ToString();
            return tableText;
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
        /// <param name="context">The context that contains information about the document being rendered</param>
        /// <returns>The created document expression</returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            if (context.Body.Count > 0)
            {
                throw new DocumentRenderException("table tags can't have a body", context.ReplacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken variableExpressionToken;

            if (reader.TryAdvance(out variableExpressionToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing collection expression after table tag", context.ReplacementKeyToken);
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
                throw new DocumentRenderException("table elements need to have at least one column parameter", context.ReplacementKeyToken);
            }

            return new TableExpression(variableExpressionToken, columns, context) { ShowDefaultValuesForArguments = showDefaults, ShowPossibleValuesForArguments = showPossibilities };
        }
    }

    

}
