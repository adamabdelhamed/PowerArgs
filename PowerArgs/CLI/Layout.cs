using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs.Cli
{
    /// <summary>
    /// Helpers for doing 2d layout
    /// </summary>
    public static class Layout
    {
        private static List<ConsoleControl> GetDescendents(ConsolePanel toTraverse)
        {
            List<ConsoleControl> ret = new List<ConsoleControl>();
            foreach (var control in toTraverse.Controls)
            {
                if (control is ConsolePanel)
                {
                    ret.AddRange(GetDescendents(control as ConsolePanel));
                }
                ret.Add(control);

            }
            return ret;
        }
        
        /// <summary>
        /// Positions the given controls in a horizontal stack
        /// </summary>
        /// <param name="margin"></param>
        /// <param name="controls"></param>
        /// <returns></returns>
        public static int StackHorizontally(int margin, IList<ConsoleControl> controls)
        {
            int left = 0;
            int width = 0;
            for(var i = 0; i < controls.Count; i++)
            {
                var control = controls[i];
                control.X = left;
                width += control.Width + (i < controls.Count - 1 ? margin : 0);
                left += control.Width + margin;
            }
            return width;
        }



        public static int StackVertically(int margin, IList<ConsoleControl> controls)
        {
            int top = 0;
            int height = 0;
            for (var i = 0; i < controls.Count; i++)
            {
                var control = controls[i];

                control.Y = top;
                height += control.Height+ (i < controls.Count - 1 ? margin : 0);
                top += control.Height + margin;
            }
            return height;
        }

        public static T CenterBoth<T>(this T child, ConsoleControl parent = null) where T : ConsoleControl => child.CenterHorizontally(parent).CenterVertically(parent);



        public static T CenterVertically<T>(this T child, ConsoleControl parent = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            
            Action syncAction = () =>
            {
                if (parent.Height == 0 || child.Height == 0) return;

                var gap = parent.Height - child.Height;
                var y = gap / 2;
                child.Y = Math.Max(0, y);
            };

            parent.SynchronizeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            child.SynchronizeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            return child;
        }

        public static T CenterHorizontally<T>(this T child, ConsoleControl parent = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;

            Action syncAction = () =>
            {
                if (parent.Width == 0 || child.Width == 0) return;

                var gap = parent.Width - child.Width;
                var x = gap / 2;
                child.X = Math.Max(0,x);
            };
            parent.SynchronizeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            child.SynchronizeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();

            return child;
        }

        public static T Fill<T>(this T child, ConsoleControl parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () =>
            {
                if (parent.Width == 0 || parent.Height== 0) return;

                var newBounds = new Rectangle(new Point(0, 0), parent.Size);
                newBounds.X += effectivePadding.Left;
                newBounds.Width -= effectivePadding.Left;
                newBounds.Width -= effectivePadding.Right;

                newBounds.Y += effectivePadding.Top;
                newBounds.Height -= effectivePadding.Top;
                newBounds.Height -= effectivePadding.Bottom;

                child.Bounds = newBounds;
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }


        public static T FillAndPreserveAspectRatio<T>(this T child, ConsoleControl parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () =>
            {
                if (parent.Width == 0 || parent.Height == 0) return;

                var aspectRatio = (float)child.Width / child.Height;
                var newW = parent.Width - (effectivePadding.Left + effectivePadding.Right);
                var newH = (int)Math.Round(newW / aspectRatio);

                if (newH > parent.Height)
                {
                    newH = parent.Height;
                    newW = (int)Math.Round(newH * aspectRatio);
                }

                var newLeft = (parent.Width - newW) / 2;
                var newTop = (parent.Height - newH) / 2;

                child.Bounds = new Rectangle(newLeft, newTop, newW, newH);
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T FillHorizontally<T>(this T child, ConsoleControl parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () => 
            {
                if (parent.Width == 0) return;

                child.Bounds = new Rectangle(effectivePadding.Left, child.Y, parent.Width - (effectivePadding.Right+effectivePadding.Left), child.Height);
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T FillVertically<T>(this T child, ConsoleControl parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () => 
            {
                child.Bounds = new Rectangle(child.X, effectivePadding.Top, child.Width, parent.Height - (effectivePadding.Top + effectivePadding.Bottom));
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T DockToBottom<T>(this T child, ConsoleControl parent = null, int padding = 0) where T :ConsoleControl
        {
            parent = parent ?? child.Parent;
            Action syncAction = () =>
            {
                if (parent.Height == 0) return;
                child.Y = parent.Height - child.Height - padding;
            };

            child.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T DockToTop<T>(this T child, ConsoleControl parent = null, int padding = 0) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            Action syncAction = () =>
            {
                child.Y = padding;
            };

            child.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T DockToRight<T>(this T child, ConsoleControl parent = null, int padding = 0) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            Action syncAction = () =>
            {
                if (parent.Width == 0 || child.Width == 0) return;

                child.X = parent.Width - child.Width - padding;
            };

            child.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T DockToLeft<T>(this T child, ConsoleControl parent = null, int padding = 0) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            Action syncAction = () =>
            {
                child.X = padding;
            };

            child.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }
    }
}

