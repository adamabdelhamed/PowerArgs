using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public class GridViewModel : Rectangular
    {
        public event Action SelectedItemActivated;
        public CollectionDataSource DataSource { get { return Get<CollectionDataSource>(); } set { Set(value); } }
        public ObservableCollection<ColumnViewModel> VisibleColumns { get; private set; }
        public GridSelectionMode SelectionMode { get { return Get<GridSelectionMode>(); } set { Set(value); } }
        public ConsoleString RowPrefix { get { return Get<ConsoleString>(); } set { Set(value); } }
        public int Gutter { get { return Get<int>(); } set { Set(value); } }

        public bool FilteringEnabled { get { return Get<bool>(); } set { Set(value); } }

        public int visibleRowOffset
        {
            get;private set;
        }

        public int NumRowsInView
        {
            get
            {
                return Height - 2;
            }
        }

        public string FilterText
        {
            get
            {
                return query.Filter;
            }
            set
            {
                query.Filter = value;
                visibleRowOffset = 0;
                SelectedIndex = 0;
                this.query.Skip = visibleRowOffset;
                DataView = DataSource.GetDataView(query);
                SelectedItem = DataView.Items.Count > 0 ? DataView.Items[0] : null;
            }
        }

        private CollectionQuery query;

        internal int selectedColumnIndex;
        internal CollectionDataView DataView
        {
            get
            {
                return Get<CollectionDataView>();
            }
            private set
            {
                Set(value);
            }
        }

        public int SelectedIndex { get { return Get<int>(); } set { Set(value); } }

        public object SelectedItem { get { return Get<object>(); } private set{ Set(value); } }


        public GridViewModel()
        {
            this.SelectionMode = GridSelectionMode.Row;
            this.RowPrefix = ConsoleString.Empty;
            this.Gutter = 3;
            this.VisibleColumns = new ObservableCollection<ColumnViewModel>();

            visibleRowOffset = 0;
            SelectedIndex = 0;
            this.PropertyChanged += MyPropertyChangedListener;
            this.query = new CollectionQuery();
        }

        

        private void MyPropertyChangedListener(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DataSource) && e.PropertyName != nameof(Bounds)) return;

            this.query.Take = NumRowsInView;
            this.query.Skip = 0;
            DataView = DataSource.GetDataView(query);
            DataSource.DataChanged += DataSource_DataChanged;
            SelectedIndex = 0;
            selectedColumnIndex = 0;
            SelectedItem = DataView.Items.Count > 0 ? DataView.Items[0] : null;
        }

        private void DataSource_DataChanged()
        {
            this.query.Skip = visibleRowOffset;
            DataView = DataSource.GetDataView(query);
            SelectedItem = DataView.Items.Count == 0 ? null : DataView.Items[SelectedIndex - visibleRowOffset];
        }

        public GridViewModel(CollectionDataSource dataSource) : this()
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

        public void MoveSelectionUpwards()
        {
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
            }

            if (SelectedIndex < visibleRowOffset)
            {
                visibleRowOffset--;
                this.query.Skip = visibleRowOffset;
                DataView = DataSource.GetDataView(query);
            }

            if (SelectedIndex - visibleRowOffset < DataView.Items.Count)
            {
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
        }

        public void PageUp()
        {
            if(SelectedIndex > visibleRowOffset)
            {
                SelectedIndex = visibleRowOffset;
            }
            else
            {
                visibleRowOffset -= NumRowsInView - 1;
                if (visibleRowOffset < 0) visibleRowOffset = 0;

                this.query.Skip = visibleRowOffset;
                DataView = DataSource.GetDataView(query);
            }

            if (SelectedIndex - visibleRowOffset < DataView.Items.Count)
            {
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
        }

        public void PageDown()
        {
            if (SelectedIndex != visibleRowOffset+DataView.Items.Count-1)
            {
                SelectedIndex = visibleRowOffset+DataView.Items.Count - 1;
            }
            else
            {
                visibleRowOffset = visibleRowOffset + DataView.Items.Count - 1;
                SelectedIndex = visibleRowOffset;
                this.query.Skip = visibleRowOffset;
                DataView = DataSource.GetDataView(query);
            }

            if (SelectedIndex - visibleRowOffset < DataView.Items.Count)
            {
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
            else if (SelectedIndex > 0)
            {
                SelectedIndex--;
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
        }

        internal void Home()
        {
            visibleRowOffset = 0;
            SelectedIndex = 0;
            this.query.Skip = visibleRowOffset;
            DataView = DataSource.GetDataView(query);

            if (SelectedIndex - visibleRowOffset < DataView.Items.Count)
            {
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
            else if (SelectedIndex > 0)
            {
                SelectedIndex--;
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
        }

        internal void End()
        {
            if (SelectedIndex == DataSource.HighestKnownIndex)
            {
                PageDown();
            }
            else
            {
                SelectedIndex = DataSource.HighestKnownIndex;
                visibleRowOffset = SelectedIndex - NumRowsInView + 1;
                if (visibleRowOffset < 0) visibleRowOffset = 0;
                this.query.Skip = visibleRowOffset;
                DataView = DataSource.GetDataView(query);

                if (SelectedIndex - visibleRowOffset < DataView.Items.Count)
                {
                    SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
                }
                else if (SelectedIndex > 0)
                {
                    SelectedIndex--;
                    SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
                }
            }
        }

        public void MoveSelectionDownwards()
        {
            if (DataView.IsLastKnownItem(SelectedItem) == false)
            {
                SelectedIndex++;
            }

            if (SelectedIndex >= visibleRowOffset + NumRowsInView)
            {
                visibleRowOffset++;
                this.query.Skip = visibleRowOffset;
                DataView = DataSource.GetDataView(query);
            }

            if (SelectedIndex - visibleRowOffset < DataView.Items.Count)
            {
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }
            else if (SelectedIndex > 0)
            {
                SelectedIndex--;
                SelectedItem = DataView.Items[SelectedIndex - visibleRowOffset];
            }

        }

        public void Activate()
        {
            if(SelectedItem != null && SelectedItemActivated != null)
            {
                SelectedItemActivated();
            }
        }

        public void MoveSelectionLeft()
        {
            if (selectedColumnIndex > 0)
            {
                selectedColumnIndex--;
            }
        }



        public void MoveSelectionRight()
        {
            if (selectedColumnIndex < VisibleColumns.Count - 1)
            {
                selectedColumnIndex++;
            }
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

        public ColumnViewModel(string columnName) : this(columnName.ToConsoleString())
        {
       
        }
    }

    public enum GridSelectionMode
    {
        Row,
        Cell,
        None,
    }
}
