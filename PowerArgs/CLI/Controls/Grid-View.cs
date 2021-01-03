using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public partial class Grid : ConsoleControl
    {
        TimerActionDebouncer filterTextDebouncer;
        private TextBox _filterTextBox;

        public TextBox FilterTextBox { get  { return _filterTextBox; } set {  SetFilterTextBox(value); } }

        public ConsoleString MoreDataMessage
        {
            get; set;
        } 
        public bool ShowEndIfComplete { get; set; } = true;

        protected override void OnPaint(ConsoleBitmap context)
        {
            PaintInternal(context);
        }

        private void PaintInternal(ConsoleBitmap context)
        {
            if (this.Height < 5)
            {
                context.DrawString("Grid can't render in a space this small", 0, 0);
                return;
            }

            if (VisibleColumns.Count == 0)
            {
                context.DrawString(NoVisibleColumnsMessage.ToConsoleString(DefaultColors.H1Color), 0, 0);
                return;
            }

            List<ConsoleString> headers = new List<ConsoleString>();
            List<List<ConsoleString>> rows = new List<List<ConsoleString>>();
            List<ColumnOverflowBehavior> overflowBehaviors = new List<ColumnOverflowBehavior>();


            if (VisibleColumns.Where(c => c.WidthPercentage != 0).Count() == 0)
            {
                foreach (var col in VisibleColumns)
                {
                    col.WidthPercentage = 1.0 / VisibleColumns.Count;
                }
            }

            foreach (var header in VisibleColumns)
            {
                headers.Add(header.ColumnDisplayName);
                var colWidth = (int)(header.WidthPercentage * this.Width);

                if (header.OverflowBehavior is SmartWrapOverflowBehavior)
                {
                    (header.OverflowBehavior as SmartWrapOverflowBehavior).MaxWidthBeforeWrapping = colWidth;
                }
                else if (header.OverflowBehavior is TruncateOverflowBehavior)
                {
                    (header.OverflowBehavior as TruncateOverflowBehavior).ColumnWidth = (header.OverflowBehavior as TruncateOverflowBehavior).ColumnWidth == 0 ? colWidth : (header.OverflowBehavior as TruncateOverflowBehavior).ColumnWidth;
                }

                overflowBehaviors.Add(header.OverflowBehavior);
            }

            int viewIndex = visibleRowOffset;
            foreach (var item in DataView.Items)
            {
                List<ConsoleString> row = new List<ConsoleString>();
                int columnIndex = 0;
                foreach (var col in VisibleColumns)
                {
                    var value = PropertyResolver(item, col.ColumnName.ToString());
                    var displayValue = value == null ? "<null>".ToConsoleString() : (value is ConsoleString ? (ConsoleString)value : value.ToString().ToConsoleString());

                    if (viewIndex == SelectedIndex && this.CanFocus)
                    {
                        if (this.SelectionMode == GridSelectionMode.Row || (this.SelectionMode == GridSelectionMode.Cell && columnIndex == selectedColumnIndex))
                        {
                            displayValue = new ConsoleString(displayValue.ToString(), this.Background, HasFocus ? DefaultColors.FocusColor : DefaultColors.SelectedUnfocusedColor);
                        }
                    }

                    row.Add(displayValue);
                    columnIndex++;
                }
                viewIndex++;
                rows.Add(row);
            }

            ConsoleTableBuilder builder = new ConsoleTableBuilder();
            ConsoleString table;
            table = builder.FormatAsTable(headers, rows, RowPrefix.ToString(), overflowBehaviors, Gutter);
            

            if (FilterText != null)
            {
                table = table.Highlight(FilterText, DefaultColors.HighlightContrastColor, DefaultColors.HighlightColor, StringComparison.InvariantCultureIgnoreCase);
            }

            if (DataView.IsViewComplete == false)
            {
                table += "Loading more rows...".ToConsoleString(DefaultColors.H1Color);
            }
            else if (DataView.IsViewEndOfData && DataView.Items.Count == 0)
            {
                table += NoDataMessage.ToConsoleString(DefaultColors.H1Color);
            }
            else if (DataView.IsViewEndOfData)
            {
                if (ShowEndIfComplete)
                {
                    table += EndOfDataMessage.ToConsoleString(DefaultColors.H1Color);
                }
            }
            else
            {
                table += MoreDataMessage;
            }
            context.DrawString(table, 0, 0);


            if (FilteringEnabled)
            {

            }
        }

        private void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.UpArrow)
            {
                Up();
            }
            else if(info.Key == ConsoleKey.DownArrow)
            {
                Down();
            }
            else if(info.Key == ConsoleKey.LeftArrow)
            {
                Left();
            }
            else if(info.Key == ConsoleKey.RightArrow)
            {
                Right();
            }
            else if(info.Key == ConsoleKey.PageDown)
            {
                PageDown();
            }
            else if(info.Key == ConsoleKey.PageUp)
            {
                PageUp();
            }
            else if(info.Key == ConsoleKey.Home)
            {
                Home();
            }
            else if(info.Key == ConsoleKey.End)
            {
                End();
            }
            else if(info.Key == ConsoleKey.Enter)
            {
                Activate();
            }
            else if(FilteringEnabled && RichTextCommandLineReader.IsWriteable(info) && FilterTextBox != null)
            {
                FilterTextBox.Value = info.KeyChar.ToString().ToConsoleString();
                Application.FocusManager.TrySetFocus(FilterTextBox);
            }
        }

        private void InitGridView()
        {
            MoreDataMessage = "more data below".ToConsoleString(DefaultColors.H1Color);
            this.KeyInputReceived.SubscribeForLifetime(OnKeyInputReceived, this);

            this.filterTextDebouncer = new TimerActionDebouncer(TimeSpan.FromSeconds(0), () =>
            {
                if (Application != null && FilterTextBox != null)
                {
                    Application.InvokeNextCycle(() =>
                    {
                        FilterText = FilterTextBox.Value.ToString();
                    });
                }
            });

            // don't accept focus unless I have at least one item in the data view
            this.Focused.SubscribeForLifetime(() =>
            {
                if (DataView.Items.Count == 0)
                {
                    Application.FocusManager.TryMoveFocus();
                }
            }, this);
        }

        private void SetFilterTextBox(TextBox value)
        {
            if(_filterTextBox != null)
            {
                throw new ArgumentException("Grid is already bound to a text box");
            }

            _filterTextBox = value;
            _filterTextBox.SubscribeForLifetime(nameof(TextBox.Value), FilterTextValueChanged, value);
            _filterTextBox.KeyInputReceived.SubscribeForLifetime(FilterTextKeyPressed, value);
            FilteringEnabled = true;
        }

        private void FilterTextKeyPressed(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Activate();
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                this.TryFocus();
            }
            else if (keyInfo.Key == ConsoleKey.PageDown)
            {
                this.TryFocus();
            }
            else if (keyInfo.Key == ConsoleKey.PageUp)
            {
                this.TryFocus();
            }
        }

        private void FilterTextValueChanged()
        {
            filterTextDebouncer.Trigger();
        }
    }
}
