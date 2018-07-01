using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public partial class Grid : ConsoleControl
    {
        private CollectionQuery query;
        private int selectedColumnIndex;
        private int visibleRowOffset;

        public event Action SelectedItemActivated;

        public CollectionDataSource DataSource { get { return Get<CollectionDataSource>(); } set { Set(value); } }
        public ObservableCollection<ColumnViewModel> VisibleColumns { get; private set; }
        public GridSelectionMode SelectionMode { get { return Get<GridSelectionMode>(); } set { Set(value); } }
        public ConsoleString RowPrefix { get { return Get<ConsoleString>(); } set { Set(value); } }
        public int Gutter { get { return Get<int>(); } set { Set(value); } }
        public string NoDataMessage { get { return Get<string>(); } set { Set(value); } }
        public string EndOfDataMessage { get { return Get<string>(); } set { Set(value); } }
        public string NoVisibleColumnsMessage { get { return Get<string>(); } set { Set(value); } }
        public bool FilteringEnabled { get { return Get<bool>(); } set { Set(value); } }
        public int NumRowsInView { get { return Height - 2; } }
        public CollectionDataView DataView { get { return Get<CollectionDataView>(); } private set { Set(value); } }
        public int SelectedIndex { get { return Get<int>(); } set { Set(value); } }
        public object SelectedItem { get { return Get<object>(); } private set{ Set(value); } }
        public Func<object, string, object> PropertyResolver { get; set; } = (item,col) => item?.GetType()?.GetProperty(col)?.GetValue(item);

        public string FilterText { get { return query.Filter; } set { SetFilterText(value); } }

        public Grid()
        {
            InitGridView();
            InitGridViewModel();
        }

        public Grid(CollectionDataSource dataSource) : this()
        {
            this.DataSource = dataSource;
        }

        public Grid(List<object> items) : this()
        {
            var prototype = items.FirstOrDefault();
            if (prototype == null) throw new InvalidOperationException("Can't infer columns without at least one item");

            foreach (var prop in prototype.GetType().GetProperties())
            {
                this.VisibleColumns.Add(new ColumnViewModel(prop.Name.ToConsoleString(Theme.DefaultTheme.H1Color)));
            }

            var dataSource = new MemoryDataSource();
            dataSource.Items = items;
            this.DataSource = dataSource;
        }

        public void Up()
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

        public void Down()
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

        public void Refresh()
        {
            DataView = DataSource.GetDataView(query);
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

        public void Home()
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

        public void End()
        {
            if (SelectedIndex == DataSource.GetHighestKnownIndex(query))
            {
                PageDown();
            }
            else
            {
                SelectedIndex = DataSource.GetHighestKnownIndex(query);
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

        public void Left()
        {
            if (selectedColumnIndex > 0)
            {
                selectedColumnIndex--;
            }
        }

        public void Right()
        {
            if (selectedColumnIndex < VisibleColumns.Count - 1)
            {
                selectedColumnIndex++;
            }
        }

        public void Activate()
        {
            if(SelectedItem != null && SelectedItemActivated != null)
            {
                SelectedItemActivated();
            }
        }

        IDisposable dataSourceSub;
        IDisposable boundsSub;
        private void InitGridViewModel()
        {
            this.SelectionMode = GridSelectionMode.Row;
            this.RowPrefix = ConsoleString.Empty;
            this.Gutter = 3;
            this.VisibleColumns = new ObservableCollection<ColumnViewModel>();

            visibleRowOffset = 0;
            SelectedIndex = 0;
            dataSourceSub = SubscribeUnmanaged(nameof(DataSource), DataSourceOrBoundsChangedListener);
            boundsSub = SubscribeUnmanaged(nameof(Bounds), DataSourceOrBoundsChangedListener);

            this.query = new CollectionQuery();

            this.NoDataMessage = "No data";
            this.EndOfDataMessage = "End";
            this.NoVisibleColumnsMessage = "No visible columns";
        }


        private void SetFilterText(string value)
        {
            query.Filter = value;
            visibleRowOffset = 0;
            SelectedIndex = 0;
            this.query.Skip = visibleRowOffset;
            if (DataSource != null)
            {
                DataView = DataSource.GetDataView(query);
                SelectedItem = DataView.Items.Count > 0 ? DataView.Items[0] : null;
            }
        }

        private void DataSourceOrBoundsChangedListener()
        {
            if (DataSource != null)
            {
                this.query.Take = NumRowsInView;
                this.query.Skip = 0;
                DataView = DataSource.GetDataView(query);
                DataSource.DataChanged += DataSourceDataChangedListener;
                SelectedIndex = 0;
                selectedColumnIndex = 0;
                SelectedItem = DataView.Items.Count > 0 ? DataView.Items[0] : null;
            }
        }

        private void DataSourceDataChangedListener()
        {
            this.query.Skip = visibleRowOffset;
            DataView = DataSource.GetDataView(query);
            SelectedItem = DataView.Items.Count == 0 ? null : DataView.Items[SelectedIndex - visibleRowOffset];
        }
    }

    public class ColumnViewModel : ObservableObject
    {
        public ConsoleString ColumnName { get; set; }
        public ConsoleString ColumnDisplayName { get { return Get<ConsoleString>(); } set { Set(value); } }

        public double WidthPercentage { get { return Get<double>(); } set { Set(value); } }

        public ColumnOverflowBehavior OverflowBehavior { get; set; }

        public ColumnViewModel(ConsoleString columnName)
        {
            this.ColumnName = columnName;
            this.ColumnDisplayName = columnName;
            this.OverflowBehavior = new GrowUnboundedOverflowBehavior();
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
