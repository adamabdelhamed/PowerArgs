using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ListView : ConsoleControl
    {
        public ListViewModel ViewModel { get; private set; }

        public event Action<ContextAssistSearchResult> Selected;

        public ListView()
        {
            ViewModel = new ListViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel_PropertyChanged(this, null);
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(this.Application != null)
            {
                this.Application.Paint();
            }

            this.CanFocus = ViewModel.IsSelectionEnabled;
        }

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            base.OnKeyInputReceived(info);
            if(ViewModel.IsSelectionEnabled == false)
            {
                return;
            }

            if(info.Key == ConsoleKey.Enter && Selected != null)
            {
                Selected(ViewModel.Items[ViewModel.SelectedIndex]);
            }
            else if(info.Key == ConsoleKey.UpArrow)
            {
                ViewModel.IncrementSelectedIndex(-1);
            }
            else if (info.Key == ConsoleKey.DownArrow)
            {
                ViewModel.IncrementSelectedIndex(1);
            }
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);

            int y = 0;
            foreach(var item in ViewModel.Items)
            {
                var displayString = this.HasFocus && y == ViewModel.SelectedIndex && ViewModel.IsSelectionEnabled ? new ConsoleString(item.DisplayText, this.FocusForeground.ForegroundColor) : item.RichDisplayText;
                context.DrawString(displayString, 0, y++);
            }
        }
    }
}
