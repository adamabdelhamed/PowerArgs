using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public class DataItemsPromise
    {
        public event Action Ready;

        public void TriggerReady()
        {
            if (Ready != null) Ready();
        }
    }

    public class DataItemsEnd
    {

    }

    public abstract class GridDataSource : ViewModelBase
    {
        public abstract List<object> GetCurrentItems(int skip, int take);
    }

    public abstract class RandomAccessDataSource : GridDataSource
    {

    }

    public class LoadMoreDataSource : GridDataSource
    {
        public override List<object> GetCurrentItems(int skip, int take)
        {
            throw new NotImplementedException();
        }
    }

    public class InMemoryDataSource : RandomAccessDataSource
    {
        public List<object> Items { get; set; }

        public InMemoryDataSource()
        {
            Items = new List<object>();
        }

        public override List<object> GetCurrentItems(int skip, int take)
        {
            return Items.Skip(skip).Take(take).ToList();
        }
    }
}
