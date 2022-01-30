using PowerArgs.Cli.Physics;
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

        public static T CenterBoth<T>(this T child, Container parent = null) where T : ConsoleControl => child.CenterHorizontally(parent).CenterVertically(parent);



        public static T CenterVertically<T>(this T child, Container parent = null) where T : ConsoleControl
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

        public static T CenterHorizontally<T>(this T child, Container parent = null) where T : ConsoleControl
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

        public static T Fill<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () =>
            {
                if (parent.Width == 0 || parent.Height== 0) return;

                var newX = 0;
                var newY = 0;
                var newW = parent.Width;
                var newH = parent.Height;
                newX += effectivePadding.Left;
                newW -= effectivePadding.Left;
                newW -= effectivePadding.Right;

                newY += effectivePadding.Top;
                newH -= effectivePadding.Top;
                newH -= effectivePadding.Bottom;

                if (newW < 0) newW = 0;
                if (newH < 0) newH = 0;

                child.Bounds = new RectF(newX, newY, newW, newH);   
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }


        public static T FillAndPreserveAspectRatio<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () =>
            {
                if (parent.Width == 0 || parent.Height == 0) return;

                var aspectRatio = (float)child.Width / child.Height;
                var newW = parent.Width - (effectivePadding.Left + effectivePadding.Right);
                var newH = ConsoleMath.Round(newW / aspectRatio);

                if (newH > parent.Height)
                {
                    newH = parent.Height;
                    newW = ConsoleMath.Round(newH * aspectRatio);
                }

                var newLeft = (parent.Width - newW) / 2;
                var newTop = (parent.Height - newH) / 2;

                child.Bounds = new RectF(newLeft, newTop, newW, newH);
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T FillHorizontally<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () => 
            {
                if (parent.Width == 0) return;
                if (parent.Width - (effectivePadding.Right + effectivePadding.Left) <= 0) return;
                child.Bounds = new RectF(effectivePadding.Left, child.Y,  parent.Width - (effectivePadding.Right+effectivePadding.Left), child.Height);
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T FillVertically<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
        {
            parent = parent ?? child.Parent;
            var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
            Action syncAction = () => 
            {
                child.Bounds = new RectF(child.X, effectivePadding.Top, child.Width, parent.Height - (effectivePadding.Top + effectivePadding.Bottom));
            };
            parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
            syncAction();
            return child;
        }

        public static T DockToBottom<T>(this T child, Container parent = null, int padding = 0) where T :ConsoleControl
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

        public static T DockToTop<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl
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

        public static T DockToRight<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl
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

        public static T DockToLeft<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl
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

