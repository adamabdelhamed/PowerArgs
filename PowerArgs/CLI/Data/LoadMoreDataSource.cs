using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
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

        private class CachedDataSet
        {
            public List<object> Items { get; private set; }

            public bool IsComplete { get; set; }

            public CachedDataSet()
            {
                Items = new List<object>();
            }
        }

        public override int GetHighestKnownIndex(CollectionQuery query)
        {
            if(cachedData.ContainsKey(query.CacheKey))
            {
                return cachedData[query.CacheKey].Items.Count - 1;
            }
            else
            {
                return 0;
            }
        }

        private Dictionary<string, CachedDataSet> cachedData;

        
        public bool HasAllDataBeenLoaded(CollectionQuery query)
        {
            CachedDataSet dataSet;
            if(cachedData.TryGetValue(query.CacheKey, out dataSet) == false)
            {
                return false;
            }
            return dataSet.IsComplete;
        }

        private CliMessagePump pump;
        private object lastContinuationToken;
        private bool isLoading;
        public LoadMoreDataSource(CliMessagePump pump)
        {
            this.pump = pump;

            this.cachedData = new Dictionary<string, CachedDataSet>();
        }

        public override void ClearCachedData()
        {
            cachedData.Clear();
        }

        public override CollectionDataView GetDataView(CollectionQuery query)
        {
            lock(cachedData)
            {
                var cacheState = GetCacheState(query);

                if (cacheState != CachedDataViewState.CompleteHit && isLoading == false && HasAllDataBeenLoaded(query) == false)
                {
                    isLoading = true;
                    pump.QueueAsyncAction(LoadMoreAsync(query, lastContinuationToken), (t) =>
                     {
                         if (t.Exception != null) throw new AggregateException(t.Exception);
                         lock (cachedData)
                         {
                             CachedDataSet results;
                             if (cachedData.TryGetValue(query.CacheKey, out results) == false)
                             {
                                 results = new CachedDataSet();
                                 results.Items.AddRange(t.Result.Items);
                                 cachedData.Add(query.CacheKey, results);
                             }
                             else
                             {
                                 results.Items.AddRange(t.Result.Items);
                             }

                             if (t.Result.ContinuationToken == null)
                             {
                                 results.IsComplete = true;
                             }
                         }

                         lastContinuationToken = t.Result.ContinuationToken;
                         isLoading = false;
                         FireDataChanged();
                     });
                }

                if (cacheState == CachedDataViewState.CompleteHit)
                {
                    return CreateFromCache(query, true, HasAllDataBeenLoaded(query) && IsEndOfCache(query));
                }
                else if (cacheState == CachedDataViewState.CompleteMiss)
                {
                    return new CollectionDataView(new List<object>(), HasAllDataBeenLoaded(query), HasAllDataBeenLoaded(query), query.Skip);
                }
                else
                {
                    return CreateFromCache(query, HasAllDataBeenLoaded(query), HasAllDataBeenLoaded(query));
                }
            }
        }

        private CollectionDataView CreateFromCache(CollectionQuery query, bool cachedPageIsComplete, bool isEndOfData)
        {
            var results = cachedData[query.CacheKey].Items.Skip(query.Skip).Take(query.Take);
            return new CollectionDataView(results.ToList(),cachedPageIsComplete,isEndOfData, query.Skip);
        }

        protected abstract Task<LoadMoreResult> LoadMoreAsync(CollectionQuery query, object continuationToken);

        private CachedDataViewState GetCacheState(CollectionQuery query)
        {
            var lastIndexRequested = query.Skip + query.Take- 1;

            CachedDataSet cachedItems;

            if(cachedData.TryGetValue(query.CacheKey, out cachedItems) == false)
            {
                return CachedDataViewState.CompleteMiss;
            }
            else if(lastIndexRequested < cachedItems.Items.Count)
            {
                return CachedDataViewState.CompleteHit; 
            }
            else if(query.Skip < cachedItems.Items.Count && cachedItems.IsComplete == false)
            {
                return CachedDataViewState.PartialHit;
            }
            else if(cachedItems.IsComplete)
            {
                return CachedDataViewState.CompleteHit;
            }
            else
            {
                return CachedDataViewState.CompleteMiss;
            }
        }

        private bool IsEndOfCache(CollectionQuery query)
        {
            CachedDataSet cachedItems;

            if (cachedData.TryGetValue(query.CacheKey, out cachedItems) == false)
            {
                return false;
            }

            var cacheState = GetCacheState(query);
            if (cacheState == CachedDataViewState.CompleteMiss) return false;
            else if (cacheState == CachedDataViewState.PartialHit) return true;
            else
            {
                return query.Skip + query.Take >= cachedItems.Items.Count;
            }
        }
    }
}
