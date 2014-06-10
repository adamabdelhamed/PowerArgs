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

        public TableExpression(DocumentToken evalToken, List<DocumentToken> columns)
        {
            this.EvalToken = evalToken;
            this.Columns = columns;
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
                headers.Add(new ConsoleString(col.Value, ConsoleColor.Yellow));
            }

            foreach(var element in collection)
            {
                var row = new List<ConsoleString>();
                foreach (var col in Columns)
                {
                    var propToGet = element.GetType().GetProperty(col.Value);
                    if (propToGet == null) throw new InvalidOperationException("'" + col.Value + "' is not a valid property for type '" + element.GetType().FullName + "' at " + col.Position);
                    var value = propToGet.GetValue(element, null);
                    row.Add(value == null ? ConsoleString.Empty : new ConsoleString(value.ToString()));
                }
                rows.Add(row);
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
            while(reader.CanAdvance(skipWhitespace: true))
            {
                columns.Add(reader.Advance(skipWhitespace: true));
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException("table elements need to have at least one column parameter");
            }

            return new TableExpression(variableExpressionToken, columns);
        }
    }
}
