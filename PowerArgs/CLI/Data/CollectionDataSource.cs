using System;

namespace PowerArgs.Cli
{
    public abstract class CollectionDataSource
    {
        public event Action DataChanged;
        public abstract CollectionDataView GetDataView(CollectionQuery query);
        public abstract int GetHighestKnownIndex(CollectionQuery query);

        protected void FireDataChanged()
        {
            if (DataChanged != null)
            {
                DataChanged();
            }
        }
    }
}
