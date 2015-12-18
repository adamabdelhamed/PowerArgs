using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class BreadcrumbBar : ConsoleControl
    {
        public PageStack PageStack { get; private set; }

        private int focusedSegmentIndex;

        public BreadcrumbBar(PageStack stack)
        {
            this.PageStack = stack;
            this.PageStack.PropertyChanged += PageStack_PropertyChanged;
            this.Height = 1;
            this.CanFocus = true;

            this.Focused += BreadcrumbBar_Focused;
            this.Unfocused += BreadcrumbBar_Unfocused;
        }



        private void BreadcrumbBar_Focused()
        {
            Application.GlobalKeyHandlers.Push(ConsoleKey.Tab, TabOverride);   
        }



        private void BreadcrumbBar_Unfocused()
        {
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Tab);
        }

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if (info.Key == ConsoleKey.Enter)
            {
                var currentSegments = PageStack.GetSegments(PageStack.CurrentPath);
                if (currentSegments.Length == 0) return;

                var newPath = "";
                for (int i = 0; i <= focusedSegmentIndex; i++)
                {
                    newPath += currentSegments[i];
                    if (i < focusedSegmentIndex)
                    {
                        newPath += "/";
                    }
                }
                PageStack.TryNavigate(newPath);
            }
        }

        private void TabOverride(ConsoleKeyInfo obj)
        {
            
            if(focusedSegmentIndex == PageStack.GetSegments(PageStack.CurrentPath).Length - 1 && obj.Modifiers.HasFlag(ConsoleModifiers.Shift) == false)
            {
                if (Application.FocusableControls.Count > 1)
                {
                    Application.MoveFocus();
                }
                else
                {
                    focusedSegmentIndex = 0;
                }
            }
            else if(focusedSegmentIndex == 0 && obj.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                if (Application.FocusableControls.Count > 1)
                {
                    Application.MoveFocus(false);
                }
                else
                {
                    focusedSegmentIndex = PageStack.GetSegments(PageStack.CurrentPath).Length - 1;
                }
            }
            else if(obj.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                focusedSegmentIndex--;
            }
            else
            {
                focusedSegmentIndex++;
            }
        }

        private void PageStack_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PageStack.CurrentPath))
            {
                Application?.Paint();
            }
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            var pages = PageStack.PagesInStack.Reverse().ToList();

            var reversePageIndex = pages.Count - 1;
            for(int y = Height-1; y >= 0 && reversePageIndex >= 0; y--)
            {
                var pageToDrawBreadcrumbFor = pages[reversePageIndex--];
                var pathString = pageToDrawBreadcrumbFor.Key;
                var segments = PageStack.GetSegments(pathString);

                var toDraw = ConsoleString.Empty;

                for(int i = 0; i < segments.Length; i++)
                {
                    if(HasFocus && focusedSegmentIndex == i)
                    {
                        toDraw += segments[i].ToConsoleString(ConsoleColor.Black, ConsoleColor.Cyan);
                    }
                    else
                    {
                        toDraw+= segments[i].ToConsoleString(ConsoleColor.Gray);
                    }

                    if(i < segments.Length-1)
                    {
                        toDraw += " -> ".ToConsoleString(ConsoleColor.Yellow);
                    }
                }

                context.DrawString(toDraw, 0, y);
            }
        }
    }
}
