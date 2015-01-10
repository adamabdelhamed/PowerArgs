using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PowerArgs.Preview
{
    /// <summary>
    /// A pipeline stage that stores all pipeline input and then formats them as a console table when the stage is drained.
    /// </summary>
    [ArgPipelineActionStage("Table")]
    public class Table : InProcessPipelineStage
    {
        /// <summary>
        /// Mostly used for testing.  An event that fires whenever a Table stage writes a table.
        /// </summary>
        public static event Action<string, List<object>, ConsoleString> TableWritten;

        private List<object> elements;
        private string[] commandLine;
        private bool passThrough;

        /// <summary>
        /// Creates a table stage
        /// </summary>
        /// <param name="commandLine">The arguments to be sent to the table expression (See TableExpression documentation).  You can also include an argument
        /// anywhere in the array with the value '$PassThrough' to indicate that pipeline objects should be forwarded to the next stage in the pipeline.</param>
        public Table(string[] commandLine) : base(commandLine) 
        {
            this.elements = new List<object>();
            this.commandLine = commandLine;

            var comparer = StringComparer.Create(CultureInfo.InvariantCulture, true);
            if(commandLine.Contains("$PassThrough", comparer))
            {
                this.commandLine = this.commandLine.Where(c => c.Equals("$PassThrough", StringComparison.InvariantCultureIgnoreCase) == false).ToArray();
                passThrough = true;
            }
        }

        /// <summary>
        /// Stores the incoming object
        /// </summary>
        /// <param name="o">a pipeline object</param>
        protected override void OnObjectReceived(object o)
        {
            lock(elements)
            {
                if(commandLine.Length == 0)
                {
                    commandLine = o.GetType().GetProperties().Where(p => p.GetGetMethod() != null).Select(p => p.Name).ToArray();
                }

                elements.Add(o);
            }
        }

        internal void ExplicitAdd(object o)
        {
            OnObjectReceived(o);
        }

        /// <summary>
        /// Writes the table and optionally passes the objects through
        /// </summary>
        protected override void BeforeSetDrainedToTrue()
        {
            bool wrapped = false;
            if (commandLine.Length == 0)
            {
                wrapped = true;
                commandLine = new string[] { "item" };
                elements = elements.Select(e => (object)new { item = e }).ToList();
            }

            DocumentRenderer renderer = new DocumentRenderer();
            var template = "{{ table elements " + string.Join(" ", commandLine) + " !}}";
            var result = renderer.Render(template, new { elements = elements });
            result.WriteLine();
            if (TableWritten != null)
            {
                TableWritten(template, elements, result);
            }

            if (passThrough == false) return;

            if(wrapped)
            {
                foreach(var item in elements)
                {
                    ArgPipeline.Push(item.GetType().GetProperty("item").GetValue(item, null), this);
                }
            }
            else
            {
                foreach (var item in elements)
                {
                    ArgPipeline.Push(item, this);
                }
            }
        }

        internal ConsoleString CreateTable()
        {
            if (commandLine.Length == 0)
            {
                commandLine = new string[] { "item" };
                elements = elements.Select(e => (object)new { item = e }).ToList();
            }

            DocumentRenderer renderer = new DocumentRenderer();
            var template = "{{ table elements " + string.Join(" ", commandLine) + " !}}";
            var result = renderer.Render(template, new { elements = elements });
            return result;
        }
    }
}
