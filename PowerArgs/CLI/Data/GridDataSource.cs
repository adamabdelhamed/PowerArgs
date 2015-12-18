using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class FilterableAttribute : Attribute { }

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

        private List<object> GetFilteredItems(string filter)
        {
            if (filter == null || filter.Length == 0) return items;
            var filteredItems = items.Where(i => (i ?? "").ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
            return filteredItems;
        }

        public override CollectionDataView GetDataView(CollectionQuery query)
        {
            lock(items)
            {
                var cacheState = GetCacheState(query);

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
                    return CreateFromCache(query, true, HasAllDataBeenLoaded && IsEndOfCache(query));
                }
                else if (cacheState == CachedDataViewState.CompleteMiss)
                {
                    return new CollectionDataView(new List<object>(), false, HasAllDataBeenLoaded, query.Skip);
                }
                else
                {
                    return CreateFromCache(query, HasAllDataBeenLoaded, HasAllDataBeenLoaded);
                }
            }
        }

        private CollectionDataView CreateFromCache(CollectionQuery query, bool cachedPageIsComplete, bool isEndOfData)
        {
            var results = GetFilteredItems(query.Filter).Skip(query.Skip).Take(query.Take);

            return new CollectionDataView(results.ToList(),cachedPageIsComplete,isEndOfData, query.Skip);
        }

        protected abstract Task<LoadMoreResult> LoadMoreAsync(object continuationToken);

        private CachedDataViewState GetCacheState(CollectionQuery query)
        {
            var lastIndexRequested = query.Skip + query.Take- 1;

            var filteredItems = GetFilteredItems(query.Filter);

            if(lastIndexRequested < filteredItems.Count)
            {
                return CachedDataViewState.CompleteHit; 
            }
            else if(query.Skip < filteredItems.Count)
            {
                return CachedDataViewState.PartialHit;
            }
            else
            {
                return CachedDataViewState.CompleteMiss;
            }
        }

        private bool IsEndOfCache(CollectionQuery query)
        {
            var cacheState = GetCacheState(query);
            if (cacheState == CachedDataViewState.CompleteMiss) return false;
            else if (cacheState == CachedDataViewState.PartialHit) return true;
            else return query.Skip + query.Take== GetFilteredItems(query.Filter).Count;
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
            IEnumerable<object> results = Items;

            if (query.Filter != null)
            {
                results = results.Where(item => MatchesFilter(item, query.Filter));
            }

            results = results.Skip(query.Skip).Take(query.Take);

            foreach (var orderBy in query.SortOrder)
            {
                if (results is IOrderedEnumerable<object>)
                {
                    if (orderBy.Descending)
                    {
                        results = (results as IOrderedEnumerable<object>).ThenByDescending(item => item?.GetType().GetProperty(orderBy.Value).GetValue(item));
                    }
                    else
                    {
                        results = (results as IOrderedEnumerable<object>).ThenBy(item => item?.GetType().GetProperty(orderBy.Value).GetValue(item));
                    }
                }
                else
                {
                    if (orderBy.Descending)
                    {
                        results = results.OrderByDescending(item => item?.GetType().GetProperty(orderBy.Value).GetValue(item));
                    }
                    else
                    {
                        results = results.OrderBy(item => item?.GetType().GetProperty(orderBy.Value).GetValue(item));
                    }
                }
            }

            return new CollectionDataView(results.ToList(), true, query.Skip + query.Take >= Items.Where(item => MatchesFilter(item, query.Filter)).Count() - 1, query.Skip);
        }

        private bool MatchesFilter(object item, string filter)
        {
            if (filter == null || filter.Length == 0) return true;

            var filterables = item.GetType().GetProperties().Where(prop => prop.HasAttr<FilterableAttribute>());

            if(filterables.Count() == 0)
            {
                return item.ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
            }
            else
            {
                foreach(var filterable in filterables)
                {
                    var propValue = filterable.GetValue(item);
                    if(propValue != null && propValue.ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
