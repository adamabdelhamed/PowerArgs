using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public class GridViewModel : ViewModelBase
    {
        public GridDataSource DataSource { get; set; }
        public ObservableCollection<ColumnViewModel> VisibleColumns { get; private set; }
        public GridSelectionMode SelectionMode { get { return Get<GridSelectionMode>(); } set { Set(value); } }
        public ConsoleString RowPrefix { get { return Get<ConsoleString>(); } set { Set(value); } }
        public int Gutter { get { return Get<int>(); } set { Set(value); } }

        public GridViewModel()
        {
            this.SelectionMode = GridSelectionMode.Cell;
            this.RowPrefix = ConsoleString.Empty;
            this.Gutter = 3;
            this.VisibleColumns = new ObservableCollection<ColumnViewModel>();
        }

        public GridViewModel(GridDataSource dataSource) : this()
        {
            this.DataSource = dataSource;
        }

        public GridViewModel(List<object> items) : this()
        {
            var prototype = items.FirstOrDefault();
            if (prototype == null) throw new InvalidOperationException("Can't infer columns without at least one item");

            foreach (var prop in prototype.GetType().GetProperties())
            {
                this.VisibleColumns.Add(new ColumnViewModel(prop.Name.ToConsoleString(ConsoleColor.Yellow)));
            }

            var dataSource = new InMemoryDataSource();
            dataSource.Items = items;
            this.DataSource = dataSource;
        }
    }

    public class ColumnViewModel : ViewModelBase
    {
        public ConsoleString ColumnName { get; private set; }
        public ConsoleString ColumnDisplayName { get { return Get<ConsoleString>(); } set { Set(value); } }

        public double WidthPercentage { get { return Get<double>(); } set { Set(value); } }

        internal ColumnOverflowBehavior OverflowBehavior { get; set; }

        public ColumnViewModel(ConsoleString columnName)
        {
            this.ColumnName = columnName;
            this.ColumnDisplayName = columnName;
            this.OverflowBehavior = new TruncateOverflowBehavior();
        }
    }

    public enum GridSelectionMode
    {
        Row,
        Cell,
        None,
    }
}
