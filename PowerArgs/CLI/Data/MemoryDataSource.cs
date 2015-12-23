using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class MemoryDataSource : CollectionDataSource
    {
        public List<object> Items { get; set; }

        public override int GetHighestKnownIndex(CollectionQuery query)
        {
            IEnumerable<object> results = Items;

            if (query.Filter != null)
            {
                results = results.Where(item => MatchesFilter(item, query.Filter));
            }

            return results.Count() - 1;
        }

        public MemoryDataSource()
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

            if (filterables.Count() == 0)
            {
                return item.ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
            }
            else
            {
                foreach (var filterable in filterables)
                {
                    var propValue = filterable.GetValue(item);
                    if (propValue != null && propValue.ToString().IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
