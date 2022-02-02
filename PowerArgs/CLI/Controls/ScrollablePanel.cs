
using PowerArgs.Cli.Physics;
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
            ScrollableContent = Add(new ConsolePanel() { IsVisible = true }).Fill();
            SynchronizeForLifetime(nameof(Background), () => ScrollableContent.Background = Background, this);
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


                var verticalScrollbarHeight = ConsoleMath.Round(Height * verticalPercentageShowing);
 
                verticalScrollbar.Height = verticalScrollbarHeight;
                verticalScrollbar.Y = ConsoleMath.Round(Height * verticalPercentageScrolled);

                if(verticalScrollbar.Y == Height && verticalPercentageScrolled < 1)
                {
                    verticalScrollbar.Y--;
                }
                else if(verticalScrollbar.Y == 0 && verticalPercentageScrolled > 0)
                {
                    verticalScrollbar.Y = 1;
                }

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

                if (verticalScrollbar.X == Width && horizontalPercentageScrolled < 1)
                {
                    verticalScrollbar.X--;
                }
                else if (verticalScrollbar.X == 0 && horizontalPercentageScrolled > 0)
                {
                    verticalScrollbar.X = 1;
                }

                horizontalScrollbar.CanFocus = true;
            }
        }



        private void FocusChanged()
        {
            bool focusedControlIsWithinMe = VisitControlTree((control) =>
            {
                if (IsExpired || IsExpiring || IsBeingRemoved) return false;
                return control is Scrollbar == false && control == Application.FocusManager.FocusedControl;
            });

            if (focusedControlIsWithinMe)
            {
                var offset = Application.FocusManager.FocusedControl.CalculateRelativePosition(this);

                var visibleWindowBounds = new RectF(HorizontalScrollUnits, VerticalScrollUnits, Width, Height);
                var focusedControlBounds = new RectF(offset.X, offset.Y, Application.FocusManager.FocusedControl.Width, Application.FocusManager.FocusedControl.Height);

                if (focusedControlBounds.IsAbove(visibleWindowBounds))
                {
                    int amount = ConsoleMath.Round(visibleWindowBounds.Top - focusedControlBounds.Top);
                    VerticalScrollUnits -= amount;
                }

                if (focusedControlBounds.IsBelow(visibleWindowBounds))
                {
                    int amount = ConsoleMath.Round(focusedControlBounds.Bottom - visibleWindowBounds.Bottom);
                    VerticalScrollUnits += amount;
                }

                if (focusedControlBounds.IsLeftOf(visibleWindowBounds))
                {
                    int amount = ConsoleMath.Round(visibleWindowBounds.Left - focusedControlBounds.Left);
                    HorizontalScrollUnits -= amount;
                }

                if (focusedControlBounds.IsRightOf(visibleWindowBounds))
                {
                    int amount = ConsoleMath.Round(focusedControlBounds.Right - visibleWindowBounds.Right);
                    HorizontalScrollUnits += amount;
                }
            }
        }



        protected override void OnPaint(ConsoleBitmap context)
        {
            ScrollableContent.Paint();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int scrollX = x + HorizontalScrollUnits;
                    int scrollY = y + VerticalScrollUnits;

                    if (scrollX >= ScrollableContent.Width || scrollY >= ScrollableContent.Height)
                    {
                        continue;
                    }

                    var scrolledPixel = ScrollableContent.Bitmap.GetPixel(scrollX, scrollY);
                    context.DrawPoint(scrolledPixel, x, y);
                }
            }

            verticalScrollbar.Paint();
            horizontalScrollbar.Paint();
            DrawScrollbar(verticalScrollbar, context);
            DrawScrollbar(horizontalScrollbar, context);

        }

        private void DrawScrollbar(Scrollbar bar, ConsoleBitmap context)
        {
            for (int x = 0; x < bar.Width; x++)
            {
                for (int y = 0; y < bar.Height; y++)
                {
                    context.DrawPoint(bar.Bitmap.GetPixel(x, y), x + bar.X, y + bar.Y);
                }
            }
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
            Background = ConsoleColor.White;
            KeyInputReceived.SubscribeForLifetime(OnKeyInputReceived, this);
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
            if (HasFocus)
            {
                context.FillRectUnsafe(new ConsoleCharacter(' ', backgroundColor: DefaultColors.FocusColor), 0, 0, Width, Height);
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
