using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class Grid : ConsoleControl
    {
        public GridViewModel ViewModel { get; set; }

        private int viewOffset, focusedIndex;

        public int MaxRowsInView
        {
            get
            {
                return Height - 2;
            }
        }

        private List<object> itemsInView = new List<object>();

        internal object FocusedItem
        {
            get
            {
                var visualIndex = focusedIndex - viewOffset;
                if (visualIndex >= itemsInView.Count)
                {
                    return null;
                }
                var ret = itemsInView[visualIndex];
                return ret;
            }
        }


        public Grid()
        {
            viewOffset = 0;
            focusedIndex = 0;
            Background = new ConsoleCharacter(' ', ConsoleColor.Red, ConsoleColor.Red);
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            if(this.Height < 5)
            {
                context.DrawString("Grid can't render in a space this small", 0, 0);
                return;
            }

            List<ConsoleString> headers = new List<ConsoleString>();
            List<List<ConsoleString>> rows = new List<List<ConsoleString>>();
            List<ColumnOverflowBehavior> overflowBehaviors = new List<ColumnOverflowBehavior>();


            if(ViewModel.VisibleColumns.Where(c => c.WidthPercentage != 0).Count() == 0)
            {
                foreach(var col in ViewModel.VisibleColumns)
                {
                    col.WidthPercentage = 1.0 / ViewModel.VisibleColumns.Count;
                }
            }

            foreach(var header in ViewModel.VisibleColumns)
            {
                headers.Add(header.ColumnDisplayName);
                var colWidth = (int)(header.WidthPercentage * this.Width);
                
                if(header.OverflowBehavior is SmartWrapOverflowBehavior)
                {
                    (header.OverflowBehavior as SmartWrapOverflowBehavior).MaxWidthBeforeWrapping = colWidth; 
                }
                else if(header.OverflowBehavior is TruncateOverflowBehavior)
                {
                    (header.OverflowBehavior as TruncateOverflowBehavior).ColumnWidth = colWidth;
                }

                overflowBehaviors.Add(header.OverflowBehavior);
            }

            int viewIndex = viewOffset;
            bool moreDataComing = false;
            bool isEnd = false;
            foreach (var item in (itemsInView = ViewModel.DataSource.GetCurrentItems(viewOffset, this.Height - 2)))
            {
                if(item is DataItemsPromise)
                {
                    (item as DataItemsPromise).Ready += () =>
                    {
                        Application.Paint();
                    };
                    moreDataComing = true;
                    break;
                }
                else if(item is DataItemsEnd)
                {
                    isEnd = true;
                    break;
                }

                List<ConsoleString> row = new List<ConsoleString>();
                foreach(var col in ViewModel.VisibleColumns)
                {
                    var value = item?.GetType()?.GetProperty(col.ColumnName.ToString())?.GetValue(item);
                    var displayValue = value == null ? "<null>".ToConsoleString() : value.ToString().ToConsoleString();

                    if(this.HasFocus && viewIndex == this.focusedIndex)
                    {
                        displayValue = new ConsoleString(displayValue.ToString(), ConsoleColor.Cyan);
                    }

                    row.Add(displayValue);
                }
                viewIndex++;
                rows.Add(row);
            }
        
            ConsoleTableBuilder builder = new ConsoleTableBuilder();
            ConsoleString table = builder.FormatAsTable(headers, rows, ViewModel.RowPrefix.ToString(), overflowBehaviors, ViewModel.Gutter);

            if(moreDataComing)
            {
                table +=  "Loading more rows...".ToConsoleString(FocusedItem is DataItemsEnd || FocusedItem is DataItemsPromise ? ConsoleColor.Cyan : ConsoleString.DefaultForegroundColor);
            }
            else if(isEnd)
            {
                table += "End of data set".ToConsoleString(FocusedItem is DataItemsEnd || FocusedItem is DataItemsPromise ? ConsoleColor.Cyan : ConsoleString.DefaultForegroundColor);
            }
            context.DrawString(table, 0, 1);
        }

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            base.OnKeyInputReceived(info);

            if(info.Key == ConsoleKey.UpArrow)
            {
                if(focusedIndex > 0)
                {
                    focusedIndex--;
                }

                if(focusedIndex < viewOffset)
                {
                    viewOffset--;
                }
            }
            else if(info.Key == ConsoleKey.DownArrow)
            {
                if(FocusedItem is DataItemsPromise || FocusedItem is DataItemsEnd)
                {
                    // do nothing
                }
                else
                {
                    focusedIndex++;
                }

                if(focusedIndex >= viewOffset + MaxRowsInView)
                {
                    viewOffset++;
                }
            }
        }

        public static void Render(List<object> data, int h = 20)
        {
            var vm = new GridViewModel(data);
            var grid = new Grid() { ViewModel = vm, Width = ConsoleProvider.Current.BufferWidth, Height = h };
            var app = new ConsoleApp(0, ConsoleProvider.Current.CursorTop, ConsoleProvider.Current.BufferWidth, h);
            app.Controls.Add(grid);
            app.Run();
        }
    }

    public class DataItemsPromise
    {
        public event Action Ready;

        public void TriggerReady()
        {
            if (Ready != null) Ready();
        }
    }

    public class DataItemsEnd
    {

    }

    public class GridViewModel : ViewModelBase
    {
        public GridDataSource DataSource { get; private set; }
        public ObservableCollection<ColumnViewModel> VisibleColumns { get; private set; }

        public ConsoleString RowPrefix { get { return Get<ConsoleString>(); } set { Set(value); } }
        public int Gutter { get { return Get<int>(); } set { Set(value); } }
        
        public GridViewModel()
        {
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

    public abstract class GridDataSource : ViewModelBase
    {
        public abstract List<object> GetCurrentItems(int skip, int take);
    }

    public abstract class RandomAccessDataSource : GridDataSource
    {
        
    }

    public class LoadMoreDataSource : GridDataSource
    {
        public override List<object> GetCurrentItems(int skip, int take)
        {
            throw new NotImplementedException();
        }
    }

    public class InMemoryDataSource : RandomAccessDataSource
    {
        public List<object> Items { get; set; }

        public InMemoryDataSource()
        {
            Items = new List<object>();
        }

        public override List<object> GetCurrentItems(int skip, int take)
        {
            return Items.Skip(skip).Take(take).ToList();
        }
    }


}
