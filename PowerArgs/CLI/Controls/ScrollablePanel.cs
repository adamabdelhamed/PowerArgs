
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ScrollablePanel : ConsolePanel
    {
        public ConsolePanel ScrollableContent;

        public Size ScrollableContentSize
        {
            get
            {
                int w = 0;
                int h = 0;

                foreach (var c in ScrollableContent.Controls.Where(c => c.IsVisible))
                {
                    w = Math.Max(w, c.X + c.Width);
                    h = Math.Max(h, c.Y + c.Height);
                }
                return new Size(w, h);
            }
        }

        public int HorizontalScrollUnits { get { return Get<int>(); } set { Set(value); } }
        public int VerticalScrollUnits { get { return Get<int>(); } set { Set(value); } }

        private PropertyChangedEventHandler focusListenHandler;

        private Scrollbar verticalScrollbar;
        private Scrollbar horizontalScrollbar;

        public ScrollablePanel()
        {
            ScrollableContent = Add(new ConsolePanel() { IsVisible = false }).Fill();
        
            verticalScrollbar = Add(new Scrollbar() { Width = 1 }).DockToRight();
            horizontalScrollbar = Add(new Scrollbar() { Height = 1 }).DockToBottom();

            ScrollableContent.Controls.Added += ScrollableControls_Added;
            ScrollableContent.Controls.Removed += ScrollableControls_Removed;
            Subscribe(nameof(HorizontalScrollUnits), UpdateScrollbars);
            Subscribe(nameof(VerticalScrollUnits), UpdateScrollbars);

        }

        private void ScrollableControls_Added(ConsoleControl c)
        {
            c.PropertyChanged += C_PropertyChanged;
        }

        private void ScrollableControls_Removed(ConsoleControl c)
        {
            c.PropertyChanged -= C_PropertyChanged;
        }

        private void C_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConsoleControl.Bounds))
            {
                UpdateScrollbars();
            }
        }

        private void UpdateScrollbars()
        {
            var contentSize = ScrollableContentSize;

            if (contentSize.Height <= Height)
            {
                verticalScrollbar.Height = 0;
                verticalScrollbar.CanFocus = false;
            }
            else
            {
                var verticalPercentageShowing = Height / (double)contentSize.Height;
                var verticalPercentageScrolled = VerticalScrollUnits / (double)contentSize.Height;
                verticalScrollbar.Height = (int)Math.Round(Height * verticalPercentageShowing);
                verticalScrollbar.Y = (int)Math.Round(Height * verticalPercentageScrolled);
                verticalScrollbar.CanFocus = true;
            }

            if (contentSize.Width <= Width)
            {
                horizontalScrollbar.Width = 0;
                horizontalScrollbar.CanFocus = false;
            }
            else
            {
                var horizontalPercentageShowing = Width / (double)contentSize.Width;
                var horizontalPercentageScrolled = HorizontalScrollUnits / (double)contentSize.Width;
                horizontalScrollbar.Width = (int)(Width * horizontalPercentageShowing);
                horizontalScrollbar.X = (int)(Width * horizontalPercentageScrolled);
                horizontalScrollbar.CanFocus = true;
            }
        }

        public override void OnAdd()
        {
            base.OnAdd();
            focusListenHandler = Application.FocusManager.Subscribe(nameof(FocusManager.FocusedControl), FocusChanged);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Application.FocusManager.Unsubscribe(focusListenHandler);
        }

        private void FocusChanged()
        {
            bool focusedControlIsWithinMe = VisitControlTree((control) =>
            {
                return control == Application.FocusManager.FocusedControl;
            });

            if (focusedControlIsWithinMe)
            {
                var offset = Application.FocusManager.FocusedControl.CalculateRelativePosition(this);

                var visibleWindowBounds = new Rectangle(HorizontalScrollUnits, VerticalScrollUnits, Width, Height);
                var focusedControlBounds = new Rectangle(offset, Application.FocusManager.FocusedControl.Size);

                if (focusedControlBounds.IsAbove(visibleWindowBounds))
                {
                    int amount = visibleWindowBounds.Top - focusedControlBounds.Top;
                    VerticalScrollUnits -= amount;
                }

                if (focusedControlBounds.IsBelow(visibleWindowBounds))
                {
                    int amount = focusedControlBounds.Bottom - visibleWindowBounds.Bottom;
                    VerticalScrollUnits += amount;
                }

                if (focusedControlBounds.IsLeftOf(visibleWindowBounds))
                {
                    int amount = visibleWindowBounds.Left - focusedControlBounds.Left;
                    HorizontalScrollUnits -= amount;
                }

                if (focusedControlBounds.IsRightOf(visibleWindowBounds))
                {
                    int amount = focusedControlBounds.Right - visibleWindowBounds.Right;
                    HorizontalScrollUnits += amount;
                }
            }
        }



        internal override void OnPaint(ConsoleBitmap context)
        {
            var fullSize = ScrollableContentSize;
            ConsoleBitmap fullyPaintedPanel = new ConsoleBitmap(0, 0, fullSize.Width, fullSize.Height);
            ScrollableContent.OnPaint(fullyPaintedPanel);

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int scrollX = x + HorizontalScrollUnits;
                    int scrollY = y + VerticalScrollUnits;

                    if (scrollX >= fullyPaintedPanel.Width || scrollY >= fullyPaintedPanel.Height)
                    {
                        continue;
                    }

                    var scrolledPixel = fullyPaintedPanel.GetPixel(scrollX, scrollY);

                    if (scrolledPixel.Value.HasValue)
                    {
                        context.Pen = scrolledPixel.Value.Value;
                    }
                    else
                    {
                        context.Pen = new ConsoleCharacter(' ', backgroundColor: Background);
                    }

                    context.DrawPoint(x, y);
                }
            }

            base.OnPaint(context);
        }
    }

    public class Scrollbar : ConsoleControl
    {
        public ScrollablePanel ScrollablePanel
        {
            get
            {
                return Parent as ScrollablePanel;
            }
        }

        public Scrollbar()
        {
            Background = ConsoleColor.DarkGray;
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            if (HasFocus)
            {
                context.Pen = new ConsoleCharacter(' ', backgroundColor: Application.Theme.FocusColor);
                context.FillRect(0, 0, Width, Height);
            }
        }

        public override bool OnKeyInputReceived(ConsoleKeyInfo info)
        {
            base.OnKeyInputReceived(info);
            var scrollableSize = ScrollablePanel.ScrollableContentSize;
            if (info.Key == ConsoleKey.Home)
            {
                ScrollablePanel.VerticalScrollUnits = 0;
                ScrollablePanel.HorizontalScrollUnits = 0;
                return true;
            }
            else if (info.Key == ConsoleKey.End)
            {
                ScrollablePanel.VerticalScrollUnits = scrollableSize.Height - ScrollablePanel.Height;
                ScrollablePanel.HorizontalScrollUnits = scrollableSize.Width - ScrollablePanel.Width;
                return true;
            }
            else if (info.Key == ConsoleKey.DownArrow)
            {
                if (ScrollablePanel.VerticalScrollUnits < scrollableSize.Height - ScrollablePanel.Height)
                {
                    ScrollablePanel.VerticalScrollUnits++;
                    return true;
                }
            }
            else if (info.Key == ConsoleKey.UpArrow)
            {
                if(ScrollablePanel.VerticalScrollUnits > 0)
                {
                    ScrollablePanel.VerticalScrollUnits--;
                    return true;
                }
            }

            return false;
        }
    }
}
