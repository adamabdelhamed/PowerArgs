using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ListViewModel : ViewModelBase
    {
        public ObservableCollection<ContextAssistSearchResult> Items { get; private set; }

        public bool IsSelectionEnabled { get{ return Get<bool>(); } set{ Set<bool>(value); } }

        public int SelectedIndex
        {
            get { return Get<int>(); }
            set
            {
                if(value < 0 || value >= Items.Count)
                {
                    throw new ArgumentOutOfRangeException("Selected index is out of range");
                }

                Set<int>(value);
            }
        }

        public ListViewModel()
        {
            IsSelectionEnabled = true;
            Items = new ObservableCollection<ContextAssistSearchResult>();
            Items.Removed += Items_Removed;
        }

        public void IncrementSelectedIndex(int amount = 1)
        {
            if (Items.Count == 0) return;

            var newIndex = SelectedIndex + amount;
            if(newIndex < 0)
            {
                newIndex = Items.Count - 1;
            }
            else if(newIndex >= Items.Count)
            {
                newIndex = 0;
            }
            SelectedIndex = newIndex;
        }

        void Items_Removed(ContextAssistSearchResult obj)
        {
            if(Items.Count == 0)
            {
                //
            }
            else if (SelectedIndex >= Items.Count)
            {
                SelectedIndex = Items.Count - 1;
            }
        }
    }
}
