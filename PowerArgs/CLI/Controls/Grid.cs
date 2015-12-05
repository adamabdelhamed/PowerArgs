using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{


    public class Grid : ConsoleControl
    {
        public GridViewModel ViewModel { get; private set; }

        public int MaxRowsInView
        {
            get
            {
                return Height - 2;
            }
        }

        private int columnViewOffset, selectedRowIndex, selectedColumnIndex;
        private List<object> itemsInView;

        internal object SelectedItem
        {
            get
            {
                var visualIndex = selectedRowIndex - columnViewOffset;
                if (visualIndex >= itemsInView.Count)
                {
                    return null;
                }
                var ret = itemsInView[visualIndex];
                return ret;
            }
        }

        public Grid(GridViewModel vm = null)
        {
            this.ViewModel = vm ?? new GridViewModel();
            itemsInView = new List<object>();
            columnViewOffset = 0;
            selectedRowIndex = 0;
            Background = new ConsoleCharacter(' ', ConsoleColor.Red, ConsoleColor.Red);
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Application?.Paint();
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

            int viewIndex = columnViewOffset;
            bool moreDataComing = false;
            bool isEnd = false;
            foreach (var item in (itemsInView = ViewModel.DataSource.GetCurrentItems(columnViewOffset, this.Height - 2)))
            {
                if(item is DataItemsPromise)
                {
                    (item as DataItemsPromise).Ready += () =>
                    {
                        Application?.Paint();
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
                int columnIndex = 0;
                foreach(var col in ViewModel.VisibleColumns)
                {
                    var value = item?.GetType()?.GetProperty(col.ColumnName.ToString())?.GetValue(item);
                    var displayValue = value == null ? "<null>".ToConsoleString() : value.ToString().ToConsoleString();

                    if(this.HasFocus && viewIndex == this.selectedRowIndex)
                    {
                        if (this.ViewModel.SelectionMode == GridSelectionMode.Row || (this.ViewModel.SelectionMode == GridSelectionMode.Cell && columnIndex == selectedColumnIndex))
                        {
                            displayValue = new ConsoleString(displayValue.ToString(), this.Background.BackgroundColor, ConsoleColor.Cyan);
                        }
                    }

                    row.Add(displayValue);
                    columnIndex++;
                }
                viewIndex++;
                rows.Add(row);
            }
        
            ConsoleTableBuilder builder = new ConsoleTableBuilder();
            ConsoleString table = builder.FormatAsTable(headers, rows, ViewModel.RowPrefix.ToString(), overflowBehaviors, ViewModel.Gutter);

            if(moreDataComing)
            {
                table +=  "Loading more rows...".ToConsoleString(SelectedItem is DataItemsEnd || SelectedItem is DataItemsPromise ? ConsoleColor.Cyan : ConsoleString.DefaultForegroundColor);
            }
            else if(isEnd)
            {
                table += "End of data set".ToConsoleString(SelectedItem is DataItemsEnd || SelectedItem is DataItemsPromise ? ConsoleColor.Cyan : ConsoleString.DefaultForegroundColor);
            }
            context.DrawString(table, 0, 0);
        }

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            base.OnKeyInputReceived(info);

            if(info.Key == ConsoleKey.UpArrow)
            {
                if(selectedRowIndex > 0)
                {
                    selectedRowIndex--;
                }

                if(selectedRowIndex < columnViewOffset)
                {
                    columnViewOffset--;
                }
            }
            else if(info.Key == ConsoleKey.DownArrow)
            {
                if(SelectedItem is DataItemsPromise || SelectedItem is DataItemsEnd)
                {
                    // do nothing
                }
                else
                {
                    selectedRowIndex++;
                }

                if(selectedRowIndex >= columnViewOffset + MaxRowsInView)
                {
                    columnViewOffset++;
                }
            }
            else if(info.Key == ConsoleKey.LeftArrow)
            {
                if(selectedColumnIndex > 0)
                {
                    selectedColumnIndex--;
                }
            }
            else if(info.Key == ConsoleKey.RightArrow)
            {
                if (selectedColumnIndex < ViewModel.VisibleColumns.Count - 1)
                {
                    selectedColumnIndex++;
                }
            }
        }
    }
}
