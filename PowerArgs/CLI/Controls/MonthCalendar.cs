using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Options for the MonthCalendar control
    /// </summary>
    public class MonthCalendarOptions
    {
        /// <summary>
        /// The year to render
        /// </summary>
        public int Year { get; set; } = DateTime.Today.Year;

        /// <summary>
        /// The month to render
        /// </summary>
        public int Month { get; set; } = DateTime.Today.Month;

        /// <summary>
        /// Specifies a color that will highlight today if it happens to be in the month. Defaults to DarkGreen.
        /// You can explicitly set this option to null if you want to disable the highlight feature.
        /// </summary>
        public RGB? TodayHighlightColor { get; set; } = RGB.DarkGreen;

        /// <summary>
        /// An optional callback that lets you customize the content area of a specific day on the calendar
        /// </summary>
        public Action<DateTime, ConsolePanel> CustomizeContent { get; set; }

        /// <summary>
        /// Sets the minimum month that the calendar will support. The
        /// built in seek functions will not allow the user to seek to an
        /// earlier month.
        /// </summary>
        public DateTime MinMonth { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Sets the maximum month that the calendar will support. The
        /// built in seek functions will not allow the user to seek to a
        /// later month.
        /// </summary>
        public DateTime MaxMonth { get; set; } = DateTime.MaxValue;
        
        /// <summary>
        /// The keyboard key that will allow the user to move the calendar
        /// to a later month when the calendar has focus. Set this option to null
        /// if you want to disable keyboard navigation and focus.
        /// </summary>
        public KeyboardShortcut AdvanceMonthForwardKey { get; set; } = new KeyboardShortcut(ConsoleKey.RightArrow, null);
        /// <summary>
        /// The keyboard key that will allow the user to move the calendar
        /// to an earlier month when the calendar has focus. Set this option to null
        /// if you want to disable keyboard navigation and focus.
        /// </summary>
        public KeyboardShortcut AdvanceMonthBackwardKey { get; set; } = new KeyboardShortcut(ConsoleKey.LeftArrow, null);
    }

    /// <summary>
    /// A control that renders a particular month and can be extended to render content on particular days
    /// </summary>
    public class MonthCalendar : ProtectedConsolePanel
    {
        private const string DayOfWeekTag = "DayOfWeekLabel";
        /// <summary>
        /// The minimum supported width of the MonthCalendar control
        /// </summary>
        public const int MinWidth = 30;
        /// <summary>
        /// /// The minimum supported Height of the MonthCalendar control
        /// </summary>
        public const int MinHeight = 15;

        private const int NumberOfRowsNeeded = 6;

        /// <summary>
        /// The options object that was passed to the constructor
        /// </summary>
        public MonthCalendarOptions Options { get; private set; }
        private GridLayout gridLayout;
        private Dictionary<DateTime, Tuple<int, int>> coordinateMemo = new Dictionary<DateTime, Tuple<int, int>>();
        private Dictionary<string, ConsolePanel> dateCells = new Dictionary<string, ConsolePanel>();

        /// <summary>
        /// Initializes the MonthCalendar control
        /// </summary>
        /// <param name="options">The options used to customize the control</param>
        public MonthCalendar(MonthCalendarOptions options = null)
        {
            var now = DateTime.Today;
            this.Options = options ?? new MonthCalendarOptions() { Year = now.Year, Month = now.Month };
            Refresh();
            SetupKeyboardInput();
        }

        /// <summary>
        /// Seeks the calendar forward or backward by a number of months.
        /// Use a positive number to go forward. Use a negative number to go backwards.
        /// </summary>
        /// <param name="numberOfMonths">the number of months to seek by</param>
        /// <returns>true if the seek happened, false if it did not because it would have gone beyond the min
        /// or max months</returns>
        public bool SeekByMonths(int numberOfMonths)
        {
            var currentDate = new DateTime(Options.Year, Options.Month, 1);
            var newDate = currentDate.AddMonths(numberOfMonths);
            if (newDate < Options.MinMonth || newDate > Options.MaxMonth) return false;

            Options.Year = newDate.Year;
            Options.Month = newDate.Month;
            Refresh();
            return true;
        }

        private void SetupKeyboardInput()
        {
            if (Options.AdvanceMonthBackwardKey == null || Options.AdvanceMonthForwardKey == null) return;
            CanFocus = true;

            this.KeyInputReceived.SubscribeForLifetime(key =>
            {
                var back = Options.AdvanceMonthBackwardKey;
                var fw = Options.AdvanceMonthForwardKey;

                var backModifierMatch = back.Modifier == null || key.Modifiers.HasFlag(back.Modifier);
                if (key.Key == back.Key && backModifierMatch) SeekByMonths(-1);

                var fwModifierMatch = fw.Modifier == null || key.Modifiers.HasFlag(fw.Modifier);
                if (key.Key == fw.Key && fwModifierMatch) SeekByMonths(1);

            }, this);

            this.Focused.SubscribeForLifetime(() =>
            {  
                Refresh();
            }, this);

            this.Unfocused.SubscribeForLifetime(() =>
            {
                Refresh();
            }, this);
        }

        /// <summary>
        /// Re-evaluates the options and re-draws the calendar
        /// </summary>
        public void Refresh()
        {
            ClearState();
            InitGridLayout();
            InitCellContainers();
            PopulateDayOfWeekLabels();
            PopulateCells();
            HighlightToday();
            SetupMinimumSizeExperience();
            PopulateMonthAndYearLabel();
        }

        private void ClearState()
        {
            this.ProtectedPanel.Controls.Clear();
            coordinateMemo.Clear();
            dateCells.Clear();
        }

        private void InitGridLayout()
        {
            var gridOptions = new GridLayoutOptions();

            for (var i = 0; i < 7; i++) gridOptions.Columns.Add(new GridColumnDefinition());

            // row for day labels, always height of 3
            gridOptions.Rows.Add(new GridRowDefinition() { Height = 3, Type = GridValueType.Pixels });

            // rows for weeks changes depending on the days of the month and the day of week of 1st day
            var rowCount = NumberOfRowsNeeded;
            for (var i = 0; i < rowCount; i++)
            {
                gridOptions.Rows.Add(new GridRowDefinition() { Height = 1, Type = GridValueType.Pixels });
            }

            // add the grid
            gridLayout = this.ProtectedPanel.Add(new GridLayout(gridOptions)).CenterBoth();
            gridLayout.RefreshLayout();

            // Adjust the dimensions of the grid layout whenever the parent size changes. This ensures that
            // all cells are the same size. If we were to just let the grid layout do its thing then it would
            // round down the last row /or column in some cases. 
            this.SynchronizeForLifetime(nameof(Bounds), () =>
            {
                var dayOfWeekHeight = 3;
                var leftOuterBorderWidth = 2;
                if (Width < MinWidth || Height < MinHeight) return;
                var cellWidth = (Width - leftOuterBorderWidth) / 7;
                var rowHeight = (Height - dayOfWeekHeight) / NumberOfRowsNeeded;

                for (var i = 0; i < gridOptions.Columns.Count; i++)
                {
                    var col = gridOptions.Columns[i];
                    col.Width = i == 0 ? cellWidth + leftOuterBorderWidth : cellWidth;
                    col.Type = GridValueType.Pixels;
                }

                var rowsAfterDayOfWeekHeaders = gridOptions.Rows.Skip(1);
                foreach (var row in rowsAfterDayOfWeekHeaders)
                {
                    row.Height = rowHeight;
                    row.Type = GridValueType.Pixels;
                }
                gridLayout.Width = leftOuterBorderWidth + (cellWidth * 7);
                gridLayout.Height = (rowHeight * NumberOfRowsNeeded) + dayOfWeekHeight;
                gridLayout.RefreshLayout();
            }, gridLayout);
        }

        /// <summary>
        /// create content panels for each cell so that other pieces of code don't need to worry about
        /// the grid layout implementation
        /// </summary>
        private void InitCellContainers()
        {
            for (var x = 0; x < gridLayout.Options.Columns.Count; x++)
            {
                // start at 1 since the first row is for day of week labels
                for (var y = 1; y < gridLayout.Options.Rows.Count; y++)
                {
                    var key = GetKeyForCoordinates(y, x);
                    var outerPanel = new ConsolePanel() { Background = Foreground };
                    var innerPanel = outerPanel.Add(new ConsolePanel() { Background = Foreground }).Fill(padding: new Thickness(x == 0 ? 2 : 0, 1, 0, 1));
                    dateCells.Add(key, innerPanel);
                    gridLayout.Add(outerPanel, x, y);

                    // the first row gets an outer border
                    if (x == 0)
                    {
                        outerPanel.Add(new ConsolePanel() { Width = 2, Background = Foreground }).FillVertically().DockToLeft();
                    }

                    outerPanel.Add(new ConsolePanel() { Width = 2, Background = Foreground }).FillVertically().DockToRight();
                    outerPanel.Add(new ConsolePanel() { Height = 1, Background = Foreground }).FillHorizontally().DockToBottom();
                }
            }
            gridLayout.RefreshLayout();
        }

        private void PopulateDayOfWeekLabels()
        {
            var dayLabels = new List<Label>();
            for (var day = 0; day < 7; day++)
            {
                var dayOfWeek = (DayOfWeek)day;
                var panel = new ConsolePanel();
                panel.Background = Foreground;
                var label = panel.Add(new Label() { Mode = LabelRenderMode.ManualSizing, Background = Foreground }).FillHorizontally(padding: new Thickness(day == 0 ? 2 : 0, 0, 0, 0)).CenterVertically();
                dayLabels.Add(label);
                gridLayout.Add(panel, day, 0);
                Func<int> smallestDayLabelWidth = () => dayLabels.Select(l => l.Width).Min();
                this.SynchronizeForLifetime(nameof(Bounds), () => label.Text = GetDayOfWeekDisplay(dayOfWeek, smallestDayLabelWidth()) , this);
            }
        }

        private void PopulateCells()
        {
            var current = new DateTime(Options.Year, Options.Month, 1);
            while (current.Month == Options.Month)
            {
                CalculateGridCoordinatesForDate(current, out int row, out int col);
                var key = GetKeyForCoordinates(row, col);
                var cellPanel = dateCells[key];
                var extensiblePanel = cellPanel.Add(new ConsolePanel()).Fill(padding: new Thickness(0, 1, 1, 0));
                Options.CustomizeContent?.Invoke(current, extensiblePanel);
                cellPanel.Background = Background;
                cellPanel.Add(new Label() { Tag = DayOfWeekTag, Text = ("" + current.Day).ToConsoleString(Foreground, Background) }).DockToTop();
                current = current.AddDays(1);
            }
        }

        private void HighlightToday()
        {
            var today = DateTime.Today;
            if (this.Options.TodayHighlightColor.HasValue && today.Year == Options.Year && today.Month == Options.Month)
            {
                var key = GetKeyForDate(DateTime.Today);
                var dayOfWeekLabel = dateCells[key].Descendents.WhereAs<Label>().Where(d => DayOfWeekTag.Equals(d.Tag)).Single();
                dayOfWeekLabel.Text = dayOfWeekLabel.Text.ToDifferentBackground(this.Options.TodayHighlightColor.Value);
            }
        }

        private Label monthAndYearLabel;
        private void PopulateMonthAndYearLabel()
        {
            var date = new DateTime(Options.Year, Options.Month, 1);
            var monthAndYearLabel = ProtectedPanel.Add(new Label() { Text = date.ToString("Y").ToConsoleString(HasFocus ? RGB.Black : Background, HasFocus ? RGB.Cyan : Foreground) });
            gridLayout.SynchronizeForLifetime(nameof(Bounds), () =>
            {
                monthAndYearLabel.X = gridLayout.X + gridLayout.Width - monthAndYearLabel.Width;
                monthAndYearLabel.Y = + gridLayout.Y + gridLayout.Height - 1;
            }, gridLayout);
        }

        private void SetupMinimumSizeExperience()
        {
            ConsolePanel shield = null;
            ConsoleControl min = null;
            min = ProtectedPanel.Add(new MinimumSizeEnforcerPanel(new MinimumSizeEnforcerPanelOptions()
            {
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                OnMinimumSizeNotMet = ()=>
                {
                    if (min == null) return;
                    min.IsVisible = false;
                    shield = ProtectedPanel.Add(new ConsolePanel() { Background = Foreground }).Fill();
                    var date = new DateTime(Options.Year, Options.Month, 1);
                    shield.Add(new Label() { Text = date.ToString("Y").ToConsoleString(Background, Foreground) }).CenterBoth();
                },
                OnMinimumSizeMet = ()=> shield?.Dispose()
            })).Fill();
        }

        private ConsoleString GetDayOfWeekDisplay(DayOfWeek day, int width)
        {
            var maxSize = Enum.GetNames(day.GetType()).Select(d => d.ToString().Length).Max();
            var fullLabelString = ("" + day).ToConsoleString(Background, Foreground);
            var abreviatedString = (fullLabelString[0] + "").ToConsoleString(Background, Foreground);
            return width < maxSize + 2 ? abreviatedString : fullLabelString;
        }
        
        private void CalculateGridCoordinatesForDate(DateTime date, out int row, out int col)
        {
            if (date.Year != Options.Year || date.Month != Options.Month) throw new InvalidOperationException("Date is not in current month");

            if (coordinateMemo.TryGetValue(date.Date, out Tuple<int, int> key) == false)
            {
                col = (int)date.DayOfWeek;
                var week = 1;
                var current = new DateTime(Options.Year, Options.Month, 1);
                while (current.Date < date.Date)
                {
                    var tomorrowIsSameMonth = current.AddDays(1).Month == Options.Month;
                    var isLastDayOfWeek = current.DayOfWeek == DayOfWeek.Saturday;
                    current = current.AddDays(1);
                    if (isLastDayOfWeek && tomorrowIsSameMonth)
                    {
                        week++;
                    }
                }

                row = week;
                coordinateMemo.Add(date.Date, new Tuple<int, int>(col, row));
            }
            else
            {
                col = key.Item1;
                row = key.Item2;
            }
        }

      
        private string GetKeyForDate(DateTime date)
        {
            CalculateGridCoordinatesForDate(date, out int row, out int col);
            return GetKeyForCoordinates(row, col);
        }

        private string GetKeyForCoordinates(int row, int col) => $"{row},{col}";
    }
}
