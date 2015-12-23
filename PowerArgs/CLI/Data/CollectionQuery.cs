using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
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

        public string CacheKey
        {
            get
            {
                return "F=" + Filter + "So=" + string.Join(", ", SortOrder.Select(se => se.Value + (se.Descending ? "-D" : "-A")));
            }
        }
    }
}
