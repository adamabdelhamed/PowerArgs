using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class TableExpression : IDocumentExpression
    {
        public DocumentToken EvalToken { get; private set; }

        public List<DocumentToken> Columns { get; private set; }

        public bool ShowPossibleValuesForArguments { get; set; }

        public bool ShowDefaultValuesForArguments { get; set; }

        public TableExpression(DocumentToken evalToken, List<DocumentToken> columns)
        {
            this.EvalToken = evalToken;
            this.Columns = columns;
            this.ShowDefaultValuesForArguments = true;
            this.ShowPossibleValuesForArguments = true;
        }

        public ConsoleString Evaluate(DataContext context)
        {
            var eval = context.EvaluateExpression(this.EvalToken.Value);

            if(eval == null)
            {
                return new ConsoleString("null", ConsoleColor.Red);
            }
            else if(eval is IEnumerable == false)
            {
                throw new InvalidOperationException("'" + this.EvalToken + "' is not enumerable at " + this.EvalToken.Position);
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

                    var propToGet = element.GetType().GetProperty(propName);
                    if (propToGet == null) throw new InvalidOperationException("'" + col.Value + "' is not a valid property for type '" + element.GetType().FullName + "' at " + col.Position);
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

    public class TableExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new InvalidOperationException("table tags can't have a body");
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableExpressionToken;

            if (reader.TryAdvance(out variableExpressionToken, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("missing collection expression");
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
                throw new InvalidOperationException("table elements need to have at least one column parameter");
            }

            return new TableExpression(variableExpressionToken, columns) { ShowDefaultValuesForArguments = showDefaults, ShowPossibleValuesForArguments = showPossibilities };
        }
    }
}
