using System.Collections.Generic;
using System.Linq;

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
}
