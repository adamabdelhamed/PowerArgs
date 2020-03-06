using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs.Cli
{
    /// <summary>
    /// Options for configuring a grid layout
    /// </summary>
    public class GridLayoutOptions
    {
        /// <summary>
        /// Column definitions
        /// </summary>
        public List<GridColumnDefinition> Columns { get; set; } = new List<GridColumnDefinition>();

        /// <summary>
        /// Row definitions
        /// </summary>
        public List<GridRowDefinition> Rows { get; set; } = new List<GridRowDefinition>();
    }

    /// <summary>
    /// The different ways for declaring the width of a column or the height
    /// of a row
    /// </summary>
    public enum GridValueType
    {
        /// <summary>
        /// The value represents a fixed number of pixels
        /// </summary>
        Pixels,
        /// <summary>
        /// The value represents a fixed percentage of the available
        /// real estate
        /// </summary>
        Percentage,
        /// <summary>
        /// The value represents a number of shares of the remaining budget
        /// after all pixel and percentage based definitions are applied. Let's say you have 4 columns.
        /// You want the first column to be 5 pixels. Now let's say the second will be 50% of the budget. 
        /// With the remaining space, you want the third column to get 2 shares and the fourth to get one share.
        /// In that case you would use RemainderValue types for the last 2 columns with values of 2 and 1 respectively.
        /// </summary>
        RemainderValue,
    }

    public abstract class GridValueDefinition
    {

        /// <summary>
        /// The type of value this definition represents
        /// </summary>
        public GridValueType Type { get; set; }

        internal abstract double Value { get; }
    }

    /// <summary>
    /// An object representation of a row height
    /// </summary>
    public class GridRowDefinition : GridValueDefinition
    {
        /// <summary>
        /// The magnitude of the height
        /// </summary>
        public double Height { get; set; }

        internal override double Value => Height;
    }

    /// <summary>
    /// An object representation of either a column width or a row height
    /// </summary>
    public class GridColumnDefinition : GridValueDefinition
    {
        /// <summary>
        /// The magnitude of the width or height
        /// </summary>
        public double Width { get; set; }

        internal override double Value => Width;

    }

    /// <summary>
    /// A control for laying out other controls in a grid layout.
    /// </summary>
    public class GridLayout : ProtectedConsolePanel
    {
        private class GridLayoutAssignment
        {
            public ConsoleControl Control { get; set; }
            public int Column { get; set; }
            public int Row { get; set; }
            public int ColumnSpan { get; set; }
            public int RowSpan { get; set; }
        }

        public GridLayoutOptions Options { get; private set; }
        private List<GridLayoutAssignment> layoutAssignments = new List<GridLayoutAssignment>();
        private int[] columnWidths;
        private int[] rowHeights;

        /// <summary>
        /// The number of columns in this grid
        /// </summary>
        public int NumColumns => Options.Columns.Count;

        /// <summary>
        /// The number of rows in this grid
        /// </summary>
        public int NumRows => Options.Rows.Count;

        /// <summary>
        /// Creates a new grid layout with the given options
        /// </summary>
        /// <param name="options">the options</param>
        public GridLayout(GridLayoutOptions options)
        {
            this.Options = options;
            this.SubscribeForLifetime(nameof(Bounds), HandleSizeChanged, this);
            ProtectedPanel.Controls.Removed.SubscribeForLifetime(HandleControlRemoved, this);
        }

        /// <summary>
        /// Gets the current column width for the given column index
        /// </summary>
        /// <param name="col">the index of the column to inspect</param>
        /// <returns>the current column width for the given column index</returns>
        public int GetColumnWidth(int col) => columnWidths[col];
        /// <summary>
        /// Gets the current row heightfor the given row index
        /// </summary>
        /// <param name="row">the index of the row to inspect</param>
        /// <returns>the current row heightfor the given row index</returns>
        public int GetRowHeight(int row) => rowHeights[row];

        /// <summary>
        /// Adds a control to the grid layout
        /// </summary>
        /// <param name="control">the control to add</param>
        /// <typeparam name="T">the type of control to add</typeparam>
        /// <param name="column">the column in which the control will be placed</param>
        /// <param name="row">thhe row in which the control will be placed</param>
        /// <param name="columnSpan">the number of columns this control should cover</param>
        /// <param name="rowSpan">the number of rows this control should cover</param>
        /// <returns>the control you added</returns>
        public T Add<T>(T control, int column, int row, int columnSpan = 1, int rowSpan = 1) where T : ConsoleControl
        {
            var assignment = new GridLayoutAssignment()
            {
                Control = control,
                Column = column,
                Row = row,
                ColumnSpan = columnSpan,
                RowSpan = rowSpan
            };
            layoutAssignments.Add(assignment);
            control.Bounds = GetCellArea(assignment);            
            ProtectedPanel.Controls.Add(control);
            

            return control;
        }

        public void Move(ConsoleControl control, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            var assignment = layoutAssignments.Where(a => a.Control == control).First();
            assignment.Column = column;
            assignment.Row = row;
            assignment.ColumnSpan = columnSpan;
            assignment.RowSpan = rowSpan;
            control.Bounds = GetCellArea(assignment);
        }

        /// <summary>
        /// Removes the given control from the layout
        /// </summary>
        /// <param name="control">the control to remove</param>
        public void Remove(ConsoleControl control)
        {
            ProtectedPanel.Controls.Remove(control);
        }

        public void Clear()
        {
            ProtectedPanel.Controls.Clear();
        }

        private void HandleControlRemoved(ConsoleControl c)
        {
            var toRemove = layoutAssignments
                .Where(a => a.Control == c)
                .FirstOrDefault();

            if (toRemove != null)
            {
                layoutAssignments.Remove(toRemove);
            }
        }

        private void HandleSizeChanged()
        {
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            this.columnWidths = ConvertDefinitionsIntoAbsolutePixelSizes(Options.Columns.Select(c => c as GridValueDefinition).ToList(), this.Width);
            this.rowHeights = ConvertDefinitionsIntoAbsolutePixelSizes(Options.Rows.Select(c => c as GridValueDefinition).ToList(), this.Height);

            foreach (var assignment in layoutAssignments)
            {
                assignment.Control.Bounds = GetCellArea(assignment);
            }
        }

        private Rectangle GetCellArea(GridLayoutAssignment assignment)
        {
            return GetCellArea(assignment.Column, assignment.Row, assignment.ColumnSpan, assignment.RowSpan);
        }

        private Rectangle GetCellArea(int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            var xOffset = 0;
            for(var x = 0; x < column; x++) { xOffset += columnWidths[x]; }
            var yOffset = 0;
            for (var y = 0; y < row; y++) { yOffset += rowHeights[y]; }

            var width = 0;
            for(var x = column; x < column+columnSpan; x++) { width += columnWidths[x]; }
            var height = 0;
            for (var y = row; y < row+ rowSpan; y++) { height+= rowHeights[y]; }

            if (height < 0) height = 0;
            if (width < 0) width = 0;

            return new Rectangle(xOffset, yOffset, width, height);
        }

        /// <summary>
        /// Takes the abstract definitions of a row or column and converts them into actual pixel
        /// values given the current budget (height or width)
        /// </summary>
        /// <param name="definitions">the definitions to evaluate</param>
        /// <param name="budget">the total number of pixels to fill</param>
        /// <returns>an array with one absolute pixel value per definition</returns>
        private int[] ConvertDefinitionsIntoAbsolutePixelSizes(List<GridValueDefinition> definitions, int budget)
        {
            Dictionary<int, int> results = new Dictionary<int, int>();
            var remainingBudget = budget;

            // handle all pixel and percentage values first, wich can easily be
            // converted into real pixel values
            for (var i = 0; i < definitions.Count; i++)
            {
                int size;
                if (definitions[i].Type == GridValueType.Pixels)
                {
                    size = (int)definitions[i].Value;
                    results.Add(i, size);
                    remainingBudget -= size;
                }
                else if (definitions[i].Type == GridValueType.Percentage)
                {
                    size = (int)Math.Round(definitions[i].Value * budget);
                    results.Add(i, size);
                    remainingBudget -= size;
                }
                else
                {
                    // remainder case, do nothing on this pass
                }
            }

            // next make a pass and count the total number of shares
            // and the total number of remainder definitions
            double remainderShares = 0;
            var numberOfRemainders = 0;
            for (var i = 0; i < definitions.Count; i++)
            {
                if(definitions[i].Type == GridValueType.RemainderValue)
                {
                    remainderShares += definitions[i].Value;
                    numberOfRemainders++;
                }
            }

            // finally, divy out the remainders and account for rounding errors
            var remainderSum = 0;
            var remaindersToProcess = numberOfRemainders;
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].Type == GridValueType.RemainderValue)
                {
                    var myShare = definitions[i].Value / remainderShares;
                    var size = (int)Math.Round(myShare * remainingBudget);
                    results.Add(i, size);
                    remainderSum += size;
                    remaindersToProcess--;

                    if(remaindersToProcess == 0)
                    {
                        // account for rounding
                        while(remainderSum < remainingBudget)
                        {
                            results[i]++;
                            remainderSum++;
                        }

                        // account for rounding
                        while (remainderSum > remainingBudget)
                        {
                            results[i]--;
                            remainderSum--;
                        }
                    }
                }
            }

            // convert results into an array
            var ret = new int[definitions.Count];
            for(var i = 0; i < ret.Length; i++)
            {
                ret[i] = results[i];
            }
            return ret;
        }
    }
}
