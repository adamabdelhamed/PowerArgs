
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

        public int HorizontalScrollUnits
        {
            get
            {
                return Get<int>();
            }
            set
            {
                if (value < 0) throw new IndexOutOfRangeException("Value must be >= 0");
                Set(value);
            }
        }
    
        public int VerticalScrollUnits
        {
            get
            {
                return Get<int>();
            }
            set
            {
                if (value < 0) throw new IndexOutOfRangeException("Value must be >= 0");

                Set(value);
            }
        }

        private IDisposable focusSubscription;

        private Scrollbar verticalScrollbar;
        private Scrollbar horizontalScrollbar;

        public ScrollablePanel()
        {
            ScrollableContent = Add(new ConsolePanel() { IsVisible = false }).Fill();
        
            verticalScrollbar = Add(new Scrollbar(Orientation.Vertical) { Width = 1 }).DockToRight();
            horizontalScrollbar = Add(new Scrollbar(Orientation.Horizontal) { Height = 1 }).DockToBottom();

            AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this);
            RemovedFromVisualTree.SubscribeForLifetime(OnRemovedFromVisualTree, this);
        }

        private void OnAddedToVisualTree()
        {
            focusSubscription = Application.FocusManager.SubscribeUnmanaged(nameof(FocusManager.FocusedControl), FocusChanged);
            SynchronizeForLifetime(nameof(HorizontalScrollUnits), UpdateScrollbars, this);
            SynchronizeForLifetime(nameof(VerticalScrollUnits), UpdateScrollbars, this);
            ScrollableContent.Controls.SynchronizeForLifetime(ScrollableControls_Added, (c)=> { }, () => { }, this);
        }

        private void OnRemovedFromVisualTree()
        {
            focusSubscription.Dispose();
        }

        private void ScrollableControls_Added(ConsoleControl c)
        {
            c.SubscribeForLifetime(nameof(Bounds), UpdateScrollbars, c);
        }
        private void UpdateScrollbars()
        {
            var contentSize = ScrollableContentSize;

            if (contentSize.Height <= Height)
            {
                verticalScrollbar.Height = 0;
                verticalScrollbar.CanFocus = false;
                VerticalScrollUnits = 0; // dangerous because if the observable is ever changed to notify on equal changes then this will cause a stack overflow
            }
            else
            {
                var verticalPercentageShowing = Height / (double)contentSize.Height;
                var verticalPercentageScrolled = VerticalScrollUnits / (double)contentSize.Height;


                var verticalScrollbarHeight = (int)Math.Round(Height * verticalPercentageShowing);
 
                verticalScrollbar.Height = verticalScrollbarHeight;
                verticalScrollbar.Y = (int)Math.Round(Height * verticalPercentageScrolled);
                verticalScrollbar.CanFocus = true;
            }

            if (contentSize.Width <= Width)
            {
                horizontalScrollbar.Width = 0;
                horizontalScrollbar.CanFocus = false;
                HorizontalScrollUnits = 0; // dangerous because if the observable is ever changed to notify on equal changes then this will cause a stack overflow
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



        protected override void OnPaint(ConsoleBitmap context)
        {
            var fullSize = ScrollableContentSize;
            ConsoleBitmap fullyPaintedPanel = new ConsoleBitmap(0, 0, fullSize.Width, fullSize.Height);
            ScrollableContent.PaintTo(fullyPaintedPanel);

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

        private Orientation orientation;

        public Scrollbar(Orientation orientation)
        {
            this.orientation = orientation;
            Background = ConsoleColor.DarkGray;
            KeyInputReceived.SubscribeForLifetime(OnKeyInputReceived, this);
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            if (HasFocus)
            {
                context.Pen = new ConsoleCharacter(' ', backgroundColor: DefaultColors.FocusColor);
                context.FillRect(0, 0, Width, Height);
            }
        }

        private void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            var scrollableSize = ScrollablePanel.ScrollableContentSize;
            if (info.Key == ConsoleKey.Home)
            {
                if (orientation == Orientation.Vertical)
                {
                    ScrollablePanel.VerticalScrollUnits = 0;
                }
                else
                {
                    ScrollablePanel.HorizontalScrollUnits = 0;
                }
            }
            else if (info.Key == ConsoleKey.End)
            {
                if (orientation == Orientation.Vertical)
                {
                    ScrollablePanel.VerticalScrollUnits = scrollableSize.Height - ScrollablePanel.Height;
                }
                else
                {
                    ScrollablePanel.HorizontalScrollUnits = scrollableSize.Width - ScrollablePanel.Width;
                }
            }
            if (info.Key == ConsoleKey.PageUp)
            {
                if (orientation == Orientation.Vertical)
                {
                    int upAmount = Math.Min(ScrollablePanel.Height, ScrollablePanel.VerticalScrollUnits);
                    ScrollablePanel.VerticalScrollUnits -= upAmount;
                }
                else
                {
                    int leftAmount = Math.Min(ScrollablePanel.Width, ScrollablePanel.HorizontalScrollUnits);
                    ScrollablePanel.HorizontalScrollUnits -= leftAmount;
                }
            }
            else if (info.Key == ConsoleKey.PageDown)
            {
                if (orientation == Orientation.Vertical)
                {
                    int downAmount = Math.Min(ScrollablePanel.Height, ScrollablePanel.ScrollableContentSize.Height - ScrollablePanel.VerticalScrollUnits - ScrollablePanel.Height);
                    ScrollablePanel.VerticalScrollUnits += downAmount;
                }
                else
                {
                    int rightAmount = Math.Min(ScrollablePanel.Width, ScrollablePanel.ScrollableContentSize.Width - ScrollablePanel.HorizontalScrollUnits - ScrollablePanel.Width);
                    ScrollablePanel.VerticalScrollUnits += rightAmount;
                }
            }
            else if (info.Key == ConsoleKey.DownArrow)
            {
                if (ScrollablePanel.VerticalScrollUnits < scrollableSize.Height - ScrollablePanel.Height)
                {
                    ScrollablePanel.VerticalScrollUnits++;
                }
            }
            else if (info.Key == ConsoleKey.UpArrow)
            {
                if(ScrollablePanel.VerticalScrollUnits > 0)
                {
                    ScrollablePanel.VerticalScrollUnits--;
                }
            }
            else if (info.Key == ConsoleKey.RightArrow)
            {
                if (ScrollablePanel.HorizontalScrollUnits < scrollableSize.Width - ScrollablePanel.Width)
                {
                    ScrollablePanel.HorizontalScrollUnits++;
                }
            }
            else if (info.Key == ConsoleKey.LeftArrow)
            {
                if (ScrollablePanel.HorizontalScrollUnits > 0)
                {
                    ScrollablePanel.HorizontalScrollUnits--;
                }
            }
        }
    }
}
