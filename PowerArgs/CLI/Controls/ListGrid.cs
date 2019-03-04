using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace PowerArgs.Cli
{


    public class ListGridOptions<T> where T : class
    {
        public bool ShowColumnHeaders { get; set; } = true;
        public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Row;
        public List<ListGridColumnDefinition<T>> Columns { get; set; }
        public ConsoleString LoadingMessage { get; set; } = "Loading...".ToConsoleString();
        public IListDataSource<T> DataSource { get; set; }
    }

    public enum DataGridSelectionMode
    {
        None,
        Row,
        Cell
    }

    public interface IListDataSource<T> where T : class
    {
        bool HasDataForRange(int min, int count);
        ListPageLoadResult<T> GetRange(int min, int count);
        Promise LoadRangeAsync(int min, int count);
    }

    public abstract class CachedRemoteList<T> : IListDataSource<T> where T : class
    {
        private Dictionary<int, T> cachedValues = new Dictionary<int, T>();
        private int? cachedCount;

        public ListPageLoadResult<T> GetRange(int min, int count)
        {
            if (cachedCount.HasValue == false) throw new InvalidOperationException("I don't have the data yet");
            var ret = new ListPageLoadResult<T>();
            ret.TotalCount = cachedCount.Value;
            for (var i = min; i < min + count; i++)
            {
                if (i < cachedCount.Value)
                {
                    ret.Items.Add(cachedValues[i]);
                }
            }
            return ret;
        }

        public bool HasDataForRange(int min, int count)
        {
            if (cachedCount.HasValue == false) return false;

            for(var i = min; i < min+count; i++)
            {
                if(i < cachedCount.Value && cachedValues.ContainsKey(i) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public Promise LoadRangeAsync(int min, int count)
        {
            var d = Deferred.Create();
            var waitCount = 1;
            Exception countException = null;
            Exception dataException = null;
            if (cachedCount.HasValue == false)
            {
                waitCount++;
                FetchCountAsync().Finally((p) =>
                {
                    if (p.Exception == null)
                    {
                        cachedCount = p.Result;
                        if (Interlocked.Decrement(ref waitCount) == 0)
                        {
                            if (dataException == null)
                            {
                                d.Resolve();
                            }
                            else
                            {
                                d.Reject(dataException);
                            }
                        }
                    }
                    else
                    {
                        countException = p.Exception;
                        if (Interlocked.Decrement(ref waitCount) == 0)
                        {
                            d.Reject(countException);
                        }
                    }
                });
            }

            FetchRangeAsync(min, count).Finally((p) =>
            {
                if (p.Exception == null)
                {
                    lock (cachedValues)
                    {
                        for (var i = 0; i < p.Result.Count; i++)
                        {
                            var bigIndex = min + i;
                            if (cachedValues.ContainsKey(bigIndex))
                            {
                                cachedValues[bigIndex] = p.Result[i];
                            }
                            else
                            {
                                cachedValues.Add(bigIndex, p.Result[i]);
                            }
                        }
                    }

                    if (Interlocked.Decrement(ref waitCount) == 0)
                    {
                        if (countException == null)
                        {
                            d.Resolve();
                        }
                        else
                        {
                            d.Reject(countException);
                        }
                    }
                }
                else
                {
                    dataException = p.Exception;
                    if (Interlocked.Decrement(ref waitCount) == 0)
                    {
                        d.Reject(countException);
                    }
                }
            });

            return d.Promise;
        }

        protected abstract Promise<int> FetchCountAsync();
        protected abstract Promise<List<T>> FetchRangeAsync(int min, int count);
    }

    public class SyncList<T> : IListDataSource<T> where T : class
    {
        private IList<T> innerList;
        public SyncList(IList<T> innerList)
        {
            this.innerList = innerList;
        }

        public ListPageLoadResult<T> GetRange(int min, int count) => new ListPageLoadResult<T>()
        {
            Items = innerList.Skip(min).Take(count).ToList(),
            TotalCount = innerList.Count,
        };

        public bool HasDataForRange(int min, int count) => true;

        public Promise LoadRangeAsync(int min, int count)
        {
            throw new NotImplementedException("This data source always has data for its range");
        }
    }

    public class ListPageLoadResult<T> where T : class
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
    }

    public class ListGridColumnDefinition<T> : DataGridColumnDefinition where T : class
    {
        public Func<T, ConsoleControl> Formatter { get; set; }
    }

    public class ListGrid<T> : ProtectedConsolePanel where T : class
    {
        private ListGridOptions<T> options;
        private int lastTopOfPageIndex;
        private int topOfPageDataIndex = 0;
        private int listCount = -1;
        private DataGridPresenter presenter;
        private Promise latestLoadingPromise;
        private Exception dataLoadException;
        private List<ConsoleControl> highlightedControls;
        private List<Lifetime> highlightLifetimes = new List<Lifetime>();
        public Event<Exception> DataLoadException { get; private set; } = new Event<Exception>();

        public Event SelectionChanged { get; private set; } = new Event();

        public int SelectedRowIndex { get => Get<int>(); set => Set(value); }
        public int SelectedColumnIndex { get => Get<int>(); set => Set(value); }

        public int PageIndex => (int)Math.Floor(topOfPageDataIndex / (double)presenter.MaxRowsThatCanBePresented);
        public int PageCount => (int)Math.Ceiling(listCount / (double)presenter.MaxRowsThatCanBePresented);

        public ListGrid(ListGridOptions<T> options) 
        {
            this.options = options;
            highlightedControls = new List<ConsoleControl>();
            CanFocus = options.SelectionMode != DataGridSelectionMode.None;
            Focused.SubscribeForLifetime(UpdateHighlightedRowsToReflectCurrentFocus, this);
            Unfocused.SubscribeForLifetime(UpdateHighlightedRowsToReflectCurrentFocus, this);
            KeyInputReceived.SubscribeForLifetime(HandleArrows, this);

            using (var modifyLock = Unlock())
            {
                presenter = Add(new DataGridPresenter(new DataGridCoreOptions()
                {
                    Columns = options.Columns.Select(c => c as DataGridColumnDefinition).ToList(),  
                    ShowColumnHeaders = options.ShowColumnHeaders,
                })).Fill();
            }

            presenter.BeforeRecompose.SubscribeForLifetime(BeforeRecompose, this);
            presenter.AfterRecompose.SubscribeForLifetime(UpdateHighlightedRowsToReflectCurrentFocus, this);
            presenter.FirstPageClicked.SubscribeForLifetime(FirstPageClicked, this);
            presenter.PreviousPageClicked.SubscribeForLifetime(PreviousPageClicked, this);
            presenter.NextPageClicked.SubscribeForLifetime(NextPageClicked, this);
            presenter.LastPageClicked.SubscribeForLifetime(LastPageClicked, this);
            this.SubscribeForLifetime(nameof(SelectedRowIndex), SelectedRowChanged, this);
            this.SubscribeForLifetime(nameof(SelectedColumnIndex), SelectedColumnChanged, this);
        }

   
 
        private void SelectedRowChanged()
        {
            if (lastTopOfPageIndex != topOfPageDataIndex)
            {
                presenter.Recompose();
            }
            else
            {
                var presentedRowIndex = SelectedRowIndex - topOfPageDataIndex;
                var rowControls = presenter.ControlsByRow[presentedRowIndex];
                highlightedControls.Clear();

                for (var i = 0; i < rowControls.Count; i++)
                {
                    if (options.SelectionMode == DataGridSelectionMode.Row || i == SelectedColumnIndex)
                    {
                        highlightedControls.Add(rowControls[i]);
                    }
                }

                Highlight(highlightedControls);
            }
            lastTopOfPageIndex = topOfPageDataIndex;
            SelectionChanged.Fire();
        }

        private void SelectedColumnChanged()
        {
            var rowControls = presenter.ControlsByRow[SelectedRowIndex - topOfPageDataIndex];
            highlightedControls.Clear();

            for (var i = 0; i < rowControls.Count; i++)
            {
                if (options.SelectionMode == DataGridSelectionMode.Row || i == SelectedColumnIndex)
                {
                    highlightedControls.Add(rowControls[i]);
                }
            }

            Highlight(highlightedControls);
            SelectionChanged.Fire();
        }

        public void Refresh()
        {
            ConsoleApp.AssertAppThread(Application);
            this.dataLoadException = null;
            latestLoadingPromise = null;
            presenter.Recompose();
        }

        private void BeforeRecompose()
        {
            highlightedControls.Clear();

            // ensure the top of the page is on a proper page boundary
            while (topOfPageDataIndex % presenter.MaxRowsThatCanBePresented != 0)
            {
                topOfPageDataIndex--;
            }

            if (options.SelectionMode != DataGridSelectionMode.None)
            {
                // ensure that the selected row is in the viewport
                while(SelectedRowIndex < topOfPageDataIndex)
                {
                    topOfPageDataIndex -= presenter.MaxRowsThatCanBePresented;
                }

                while(SelectedRowIndex >= topOfPageDataIndex+presenter.MaxRowsThatCanBePresented)
                {
                    topOfPageDataIndex += presenter.MaxRowsThatCanBePresented;
                }
            }


            if (dataLoadException != null)
            {
                presenter.Options.IsLoading = true;
                presenter.Options.LoadingMessage = "Failed to load data".ToRed();
            }
            else if(options.DataSource.HasDataForRange(topOfPageDataIndex, presenter.MaxRowsThatCanBePresented))
            {
                var range = options.DataSource.GetRange(topOfPageDataIndex, presenter.MaxRowsThatCanBePresented);
                this.listCount = range.TotalCount;
                presenter.Options.Rows = new List<DataGridPresentationRow>();
                presenter.Options.IsLoading = false;
                for(var i = 0; i < range.Items.Count; i++)
                {
                    var item = range.Items[i];
                    var deepIndex = i + topOfPageDataIndex;
                    var row = new DataGridPresentationRow();
                    presenter.Options.Rows.Add(row);
                    for(var j = 0; j < options.Columns.Count; j++)
                    {
                        var col = options.Columns[j];

                        bool shouldBeHighlighted = false;

                        if(options.SelectionMode == DataGridSelectionMode.Row && deepIndex == SelectedRowIndex)
                        {
                            shouldBeHighlighted = true;
                        }
                        else if(options.SelectionMode == DataGridSelectionMode.Cell && deepIndex == SelectedRowIndex && SelectedColumnIndex == j)
                        {
                            shouldBeHighlighted = true;
                        }

                        row.Cells.Add(() =>
                        {
                            var control = col.Formatter(item);
                            if (shouldBeHighlighted)
                            {
                                highlightedControls.Add(control);
                            }
                            return control;
                        });
                    }
                }

                presenter.Options.PagerState = new PagerState()
                {
                    AllowRandomAccess = true,
                    CanGoBackwards = PageIndex > 0,
                    CanGoForwards = PageIndex < PageCount - 1,
                    CurrentPageLabelValue = $"Page {PageIndex + 1} of {PageCount}".ToConsoleString(),
                };
            }
            else
            {
                presenter.Options.IsLoading = true;
                presenter.Options.LoadingMessage = options.LoadingMessage;
                var myPromise = options.DataSource.LoadRangeAsync(topOfPageDataIndex, presenter.MaxRowsThatCanBePresented);

                myPromise.Then(() => Application.QueueAction(() =>
                {
                    if (myPromise == latestLoadingPromise)
                    {
                        Application.QueueAction(presenter.Recompose);
                    }
                }));

                myPromise.Fail((ex)=> Application.QueueAction(()=>
                {
                    if (myPromise == latestLoadingPromise)
                    {
                        dataLoadException = ex;
                        presenter.Recompose();
                    }
                }));
                latestLoadingPromise = myPromise;
            }
        }

        private void UpdateHighlightedRowsToReflectCurrentFocus()
        {
            Highlight(highlightedControls);
        }

        private void Highlight(List<ConsoleControl> controls)
        {
            foreach (var lifetime in highlightLifetimes)
            {
                lifetime.Dispose();
            }

            highlightLifetimes.Clear();

            foreach (var cellDisplayControl in controls)
            {
                var highlightLifetime = new Lifetime();
                highlightLifetimes.Add(highlightLifetime);
                if (cellDisplayControl is Label)
                {
                    var label = (cellDisplayControl as Label);
                    var originalText = label.Text;
                    label.Text = label.Text.ToBlack().ToDifferentBackground(HasFocus ? ConsoleColor.Cyan : ConsoleColor.DarkGray);
                    highlightLifetime.OnDisposed(() =>
                    {
                        if (label.IsExpired == false)
                        {
                            label.Text = originalText;
                        }
                    });
                }
                else
                {
                    var originalFg = cellDisplayControl.Foreground;
                    var originalBg = cellDisplayControl.Background;
                    cellDisplayControl.Foreground = ConsoleColor.White;
                    cellDisplayControl.Background = HasFocus ? ConsoleColor.Cyan : ConsoleColor.DarkGray;
                    highlightLifetime.OnDisposed(() =>
                    {
                        if (cellDisplayControl.IsExpired == false)
                        {
                            cellDisplayControl.Foreground = originalFg;
                            cellDisplayControl.Background = originalBg;
                        }
                    });
                }
            }
        }

        private void FirstPageClicked()
        {
            topOfPageDataIndex = 0;
            SelectedRowIndex = topOfPageDataIndex;
        }

        private void PreviousPageClicked()
        {
            topOfPageDataIndex = Math.Max(0, topOfPageDataIndex - presenter.MaxRowsThatCanBePresented);
            SelectedRowIndex = topOfPageDataIndex;
        }

        private void NextPageClicked()
        {
            topOfPageDataIndex = Math.Min(listCount - 1, topOfPageDataIndex + presenter.MaxRowsThatCanBePresented);
            SelectedRowIndex = topOfPageDataIndex;
        }

        private void LastPageClicked()
        {
            topOfPageDataIndex = (PageCount - 1) * presenter.MaxRowsThatCanBePresented;
            SelectedRowIndex = topOfPageDataIndex;
        }

        private void HandleArrows(ConsoleKeyInfo keyInfo)
        {
            if (presenter.Options.IsLoading)
            {
                return;
            }

            if (options.SelectionMode != DataGridSelectionMode.None)
            {
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (SelectedRowIndex > 0)
                    {
                        if (SelectedRowIndex == topOfPageDataIndex)
                        {
                            topOfPageDataIndex = Math.Max(0, SelectedRowIndex - presenter.MaxRowsThatCanBePresented);

                        }

                        SelectedRowIndex--;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    if (SelectedRowIndex < listCount - 1)
                    {
                        if (SelectedRowIndex == topOfPageDataIndex + presenter.MaxRowsThatCanBePresented - 1)
                        {
                            topOfPageDataIndex = SelectedRowIndex + 1;
                        }
                        SelectedRowIndex++;
                    }
                }
            }

            if (options.SelectionMode == DataGridSelectionMode.Cell)
            {
                if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (SelectedColumnIndex > 0)
                    {
                        SelectedColumnIndex--;
                    }
                }
                else if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (SelectedColumnIndex < options.Columns.Count - 1)
                    {
                        SelectedColumnIndex++;
                    }
                }
            }
        }
    }
}
