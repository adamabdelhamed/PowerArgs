using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class CollectionDataView
    {
        public bool IsViewComplete { get; private set; }
        public bool IsViewEndOfData { get; private set; }
        public int RowOffset { get; private set; }
        public IReadOnlyList<object> Items { get; private set; }

        public CollectionDataView(List<object> items, bool isCompletelyLoaded, bool isEndOfData, int rowOffset)
        {
            this.Items = items.AsReadOnly();
            this.IsViewComplete = isCompletelyLoaded;
            this.IsViewEndOfData = isEndOfData;
            this.RowOffset = rowOffset;
        }

        public bool IsLastKnownItem(object item)
        {
            if (Items.Count == 0) return item == null;
            if (object.ReferenceEquals(item, Items.Last()) == false) return false;

            if (IsViewComplete == false || IsViewEndOfData)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class SortExpression
    {
        public string Value { get; set; }
        public bool Descending { get; set; }

        public SortExpression(string value, bool descending = false)
        {
            this.Value = value;
            this.Descending = descending;
        }
    }

    public class CollectionQuery
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public string Filter { get; set; }
        public List<SortExpression> SortOrder { get; private set; }

        public CollectionQuery()
        {
            SortOrder = new List<SortExpression>();
        }

        public CollectionQuery(int skip, int take, string filter, params SortExpression[] sortOrder) : this()
        {
            this.Skip = skip;
            this.Take = take;
            this.Filter = filter;
            this.SortOrder.AddRange(sortOrder);
        }
    }

    public abstract class CollectionDataSource : ViewModelBase
    {
        public event Action DataChanged;
        public abstract CollectionDataView GetDataView(CollectionQuery query);

        public abstract int HighestKnownIndex { get; }

        protected void FireDataChanged()
        {
            if (DataChanged != null)
            {
                DataChanged();
            }
        }
    }

    public abstract class RandomAccessDataSource : CollectionDataSource
    {

    }

    public abstract class LoadMoreDataSource : CollectionDataSource
    {
        private enum CachedDataViewState
        {
            CompleteMiss,
            PartialHit,
            CompleteHit
        }

        public class LoadMoreResult
        {
            public List<object> Items { get; private set; }
            public object ContinuationToken { get; private set; }

            public LoadMoreResult(List<object> items, object continuationToken)
            {
                this.Items = items;
                this.ContinuationToken = continuationToken;
            }
        }

        public override int HighestKnownIndex
        {
            get
            {
                return items.Count - 1;
            }
        }

        private List<object> items;
        public bool HasAllDataBeenLoaded { get; private set; }
        private CliMessagePump pump;
        private object lastContinuationToken;
        private bool isLoading;
        public LoadMoreDataSource(CliMessagePump pump)
        {
            this.pump = pump;
            this.items = new List<object>();
        }

  

        public override CollectionDataView GetDataView(CollectionQuery query)
        {
            lock(items)
            {
                var cacheState = GetCacheState(query.Skip, query.Take);

                if (cacheState != CachedDataViewState.CompleteHit && isLoading == false && HasAllDataBeenLoaded == false)
                {
                    isLoading = true;
                    var loadMoreTask = LoadMoreAsync(lastContinuationToken);
                    loadMoreTask.ContinueWith((t) =>
                    {
                        pump.QueueAction(() =>
                        {
                            if (t.Exception != null) throw new AggregateException(t.Exception);
                            lock(items)
                            {
                                items.AddRange(t.Result.Items);
                            }
                            if (t.Result.ContinuationToken == null)
                            {
                                HasAllDataBeenLoaded = true;
                            }
                            lastContinuationToken = t.Result.ContinuationToken;
                            isLoading = false;
                            FireDataChanged();
                        });
                    });
                }

                if (cacheState == CachedDataViewState.CompleteHit)
                {
                    return new CollectionDataView(items.Skip(query.Skip).Take(query.Take).ToList(), true, HasAllDataBeenLoaded && IsEndOfCache(query.Skip, query.Take), query.Skip);
                }
                else if (cacheState == CachedDataViewState.CompleteMiss)
                {
                    return new CollectionDataView(new List<object>(), false, HasAllDataBeenLoaded, query.Skip);
                }
                else
                {
                    return new CollectionDataView(items.Skip(query.Skip).Take(query.Take).ToList(), HasAllDataBeenLoaded, HasAllDataBeenLoaded, query.Skip);
                }
            }
        }

        protected abstract Task<LoadMoreResult> LoadMoreAsync(object continuationToken);

        private CachedDataViewState GetCacheState(int skip, int take)
        {
            var lastIndexRequested = skip + take - 1;
            if(lastIndexRequested < items.Count)
            {
                return CachedDataViewState.CompleteHit; 
            }
            else if(skip < items.Count)
            {
                return CachedDataViewState.PartialHit;
            }
            else
            {
                return CachedDataViewState.CompleteMiss;
            }
        }

        private bool IsEndOfCache(int skip, int take)
        {
            var cacheState = GetCacheState(skip, take);
            if (cacheState == CachedDataViewState.CompleteMiss) return false;
            else if (cacheState == CachedDataViewState.PartialHit) return true;
            else return skip + take == items.Count;
        }
    }

    public class InMemoryDataSource : RandomAccessDataSource
    {
        public List<object> Items { get; set; }

        public override int HighestKnownIndex
        {
            get
            {
                return Items.Count - 1;
            }
        }

        public InMemoryDataSource()
        {
            Items = new List<object>();
        }

        public void Invalidate()
        {
            FireDataChanged();
        }

        public override CollectionDataView GetDataView(CollectionQuery query)
        {
            return new CollectionDataView(Items.Skip(query.Skip).Take(query.Take).ToList(), true, query.Skip + query.Take >= Items.Count - 1, query.Skip);
        }
    }
}
