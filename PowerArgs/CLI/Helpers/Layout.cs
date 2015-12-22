using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public static class Layout
    {
        public static void CenterVertically(Rectangular parent, Rectangular child)
        {
            var gap = parent.Height - child.Height;
            var y = gap / 2;
            child.Y = y;
        }

        public static void CenterHorizontally(Rectangular parent, Rectangular child)
        {
            var gap = parent.Width - child.Width;
            var x = gap / 2;
            child.X = x;
        }

        public static List<ConsoleControl> TraverseControlTree(ConsolePanel toTraverse)
        {
            List<ConsoleControl> ret = new List<ConsoleControl>();
            foreach (var control in toTraverse.Controls)
            {
                if (control is ConsolePanel)
                {
                    ret.AddRange(TraverseControlTree(control as ConsolePanel));
                }
                ret.Add(control);

            }
            return ret;
        }

        public static void StackHorizontally(int margin, IEnumerable<ConsoleControl> controls)
        {
            StackHorizontally(margin, controls.ToArray());
        }

        public static void StackHorizontally(int margin, params ConsoleControl[] controls)
        {
            int left = 0;
            foreach(var control in controls)
            {
                control.X = left;
                left += control.Width + margin;
            }
        }

        public static void StackVertically(int margin, params ConsoleControl[] controls)
        {
            int top = 0;
            foreach (var control in controls)
            {
                control.Y = top;
                top += control.Height + margin;
            }
        }

        public static void Fill(ConsoleControl child, ConsolePanel parent)
        {
            parent.PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == nameof(ConsoleControl.Bounds))
                {
                    child.Bounds = new Rectangle(new Point(0, 0), parent.Size);
                }
            };
        }
    }
}

