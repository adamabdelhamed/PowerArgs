using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    internal class DataGridCoreOptions
    {
        public bool ShowColumnHeaders { get; set; } = true;
        public bool ShowPager { get; set; } = true;
        public List<DataGridColumnDefinition> Columns { get; set; }
        public List<DataGridPresentationRow> Rows { get; set; }
        public PagerState PagerState { get; set; }
        public ConsoleString LoadingMessage { get; set; }
        public bool IsLoading { get; set; }
    }

    internal class DataGridPresentationRow
    {
        public List<Func<ConsoleControl>> Cells { get; set; } = new List<Func<ConsoleControl>>();
    }


    public class DataGridColumnDefinition : GridColumnDefinition
    {
        public ConsoleString Header { get; set; }
    }

    internal class PagerState
    {
        public bool AllowRandomAccess { get; set; }
        public bool CanGoBackwards { get; set; }
        public bool CanGoForwards { get; set; }
        public ConsoleString CurrentPageLabelValue { get; set; }
    }

    internal class DataGridPresenter : ProtectedConsolePanel 
    {
        private GridLayout gridLayout;
        private ConsolePanel pagerContainer;
        private ConsolePanel loadingPanel;
        public DataGridCoreOptions Options { get; private set; }
        private List<ConsoleControl> recomposableControls;

        public Dictionary<int, List<ConsoleControl>> ControlsByRow { get; private set; } = new Dictionary<int, List<ConsoleControl>>();

        private RandomAccessPager pager;

        public Event FirstPageClicked { get; private set; } = new Event();
        public Event PreviousPageClicked { get; private set; } = new Event();
        public Event NextPageClicked { get; private set; } = new Event();
        public Event LastPageClicked { get; private set; } = new Event();
        public Event BeforeRecompose { get; private set; } = new Event();
        public Event AfterRecompose { get; private set; } = new Event();
        public int MaxRowsThatCanBePresented => Options.ShowColumnHeaders ? Height - 2 : Height - 1;

        public DataGridPresenter(DataGridCoreOptions options)
        {
            this.Options = options;
            recomposableControls = new List<ConsoleControl>();

            var columns = options.Columns.Select(c => c as GridColumnDefinition).ToList();

            if(columns.Where(c => c.Type == GridValueType.RemainderValue).Count() == 0)
            {
                columns.Add(new GridColumnDefinition()
                {
                    Type = GridValueType.RemainderValue,
                    Width = 1
                });
            }

            gridLayout = ProtectedPanel.Add(new GridLayout(new GridLayoutOptions()
            {
                Columns = columns,
                Rows = new List<GridRowDefinition>()
            })).Fill();

            SubscribeForLifetime(nameof(Bounds), Recompose, this);
        }

        public void Recompose()
        {
            BeforeRecompose.Fire();
            SnapshotPagerFocus();
            Decompose();
            ComposeGridLayout();

            if (Options.IsLoading)
            {
                ComposeLoadingUX();
            }
            else
            {
                ComposeDataCells();
                ComposePager();
            }
            AfterRecompose.Fire();
        }

        private bool firstButtonFocused, previousButtonFocused, nextButtonFocused, lastButtonFocused;

        private void SnapshotPagerFocus()
        {
            firstButtonFocused = pager != null && pager.FirstPageButton.HasFocus;
            previousButtonFocused = pager != null && pager.PreviousPageButton.HasFocus;
            nextButtonFocused = pager != null && pager.NextPageButton.HasFocus;
            lastButtonFocused = pager != null && pager.LastPageButton.HasFocus;
        }

        private void Decompose()
        {
            for (var i = 0; i < recomposableControls.Count; i++)
            {
                gridLayout.Remove(recomposableControls[i]);
            }
            recomposableControls.Clear();
            ControlsByRow.Clear();

            if(loadingPanel != null)
            {
                ProtectedPanel.Controls.Remove(loadingPanel);
                loadingPanel = null;
            }
        }


        private void ComposeGridLayout()
        {
            gridLayout.Options.Rows = new List<GridRowDefinition>();
            for (var i = 0; i < Height; i++)
            {
                gridLayout.Options.Rows.Add(new GridRowDefinition() {Height= 1, Type = GridValueType.Pixels });
            }
            gridLayout.RefreshLayout();
        }

        private void ComposeLoadingUX()
        {
            loadingPanel = ProtectedPanel.Add(new ConsolePanel() { ZIndex = int.MaxValue }).Fill();
            loadingPanel.Add(new Label() { Text = Options.LoadingMessage }).CenterBoth();
        }

        private void ComposeDataCells()
        {
            if (Options.ShowColumnHeaders)
            {
                for (var col = 0; col < Options.Columns.Count; col++)
                {
                    recomposableControls.Add(gridLayout.Add(new Label() { Text = Options.Columns[col].Header }, col, 0));
                }
            }

            var dataRowStartIndex = Options.ShowColumnHeaders ? 1 : 0;
            var currentIndex = 0;
            for (var gridLayoutRow = dataRowStartIndex; gridLayoutRow < dataRowStartIndex + MaxRowsThatCanBePresented; gridLayoutRow++)
            {
                if (currentIndex >= Options.Rows.Count) break;
                var dataItem = Options.Rows[currentIndex];
                var rowControls = new List<ConsoleControl>();
                ControlsByRow.Add(currentIndex, rowControls);
                for (var gridLayoutCol = 0; gridLayoutCol < Options.Columns.Count; gridLayoutCol++)
                {
                    var columnDefinition = Options.Columns[gridLayoutCol];
                    var cellDisplayControl = gridLayout.Add(dataItem.Cells[gridLayoutCol].Invoke(), gridLayoutCol, gridLayoutRow);
                    recomposableControls.Add(cellDisplayControl);
                    rowControls.Add(cellDisplayControl);
                    
                }
                currentIndex++;
            }
        }

        private void ComposePager()
        {
            pagerContainer = gridLayout.Add(new ConsolePanel(), 0, Height-1, gridLayout.Options.Columns.Count, 1);
            recomposableControls.Add(pagerContainer);
            pager = pagerContainer.Add(new RandomAccessPager()).CenterHorizontally();
            pager.IsVisible = Options.ShowPager;
            pager.FirstPageButton.Pressed.SubscribeForLifetime(FirstPageClicked.Fire, pager);
            pager.PreviousPageButton.Pressed.SubscribeForLifetime(PreviousPageClicked.Fire, pager);
            pager.NextPageButton.Pressed.SubscribeForLifetime(NextPageClicked.Fire, pager);
            pager.LastPageButton.Pressed.SubscribeForLifetime(LastPageClicked.Fire, pager);
            pager.FirstPageButton.CanFocus = Options.PagerState.CanGoBackwards;
            pager.PreviousPageButton.CanFocus = Options.PagerState.CanGoBackwards;
            pager.NextPageButton.CanFocus = Options.PagerState.CanGoForwards;
            pager.LastPageButton.CanFocus = Options.PagerState.CanGoForwards;
            pager.CurrentPageLabel.Text = Options.PagerState.CurrentPageLabelValue;
            if (Options.PagerState.AllowRandomAccess == false)
            {
                pager.Controls.Remove(pager.LastPageButton);
            }

            if (firstButtonFocused) pager.FirstPageButton.TryFocus();
            else if (previousButtonFocused) pager.PreviousPageButton.TryFocus();
            else if(nextButtonFocused) pager.NextPageButton.TryFocus();
            else if(lastButtonFocused) pager.LastPageButton.TryFocus();
        }

        private class RandomAccessPager : StackPanel
        {
            public Button FirstPageButton { get; private set; }
            public Button PreviousPageButton { get; private set; }
            public Label CurrentPageLabel { get; private set; }
            public Button NextPageButton { get; private set; }
            public Button LastPageButton { get; private set; }

            public RandomAccessPager()
            {
                AutoSize = true;
                Margin = 2;
                Orientation = Orientation.Horizontal;
                FirstPageButton = Add(new Button() { Text = "<<".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.Home) });
                PreviousPageButton = Add(new Button() { Text = "<".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.PageUp) });
                CurrentPageLabel = Add(new Label() { Text = "Page 1 of 1".ToConsoleString() });
                NextPageButton = Add(new Button() { Text = ">".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.PageDown) });
                LastPageButton = Add(new Button() { Text = ">>".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.End) });
            }
        }
    }
}
