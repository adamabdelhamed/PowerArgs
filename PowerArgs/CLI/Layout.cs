using PowerArgs.Cli.Physics;
namespace PowerArgs.Cli;
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
        for (var i = 0; i < controls.Count; i++)
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
            height += control.Height + (i < controls.Count - 1 ? margin : 0);
            top += control.Height + margin;
        }
        return height;
    }

    public static T CenterBoth<T>(this T child, Container parent = null) where T : ConsoleControl => 
        child.CenterHorizontally(parent).CenterVertically(parent);


    public static T CenterVertically<T>(this T child, Container parent = null) where T : ConsoleControl
    {
        return DoTwoWayLayoutAction(child, parent, (c, p) =>
        {
            if (p.Height == 0 || c.Height == 0) return;
            var gap = p.Height - c.Height;
            var y = gap / 2;
            c.Y = Math.Max(0, y);
        });
    }

    public static T CenterHorizontally<T>(this T child, Container parent = null) where T : ConsoleControl
    {
        return DoTwoWayLayoutAction(child, parent, (c, p) =>
        {
            if (p.Width == 0 || c.Width == 0) return;
            var gap = p.Width - c.Width;
            var x = gap / 2;
            c.X = Math.Max(0, x);
        });
    }

    public static T Fill<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
    {
        var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
        return DoParentTriggeredLayoutAction(child, parent, (c, p) =>
        {
            if (p.Width == 0 || p.Height == 0) return;

            var newX = 0;
            var newY = 0;
            var newW = p.Width;
            var newH = p.Height;
            newX += effectivePadding.Left;
            newW -= effectivePadding.Left;
            newW -= effectivePadding.Right;

            newY += effectivePadding.Top;
            newH -= effectivePadding.Top;
            newH -= effectivePadding.Bottom;

            if (newW < 0) newW = 0;
            if (newH < 0) newH = 0;

            c.Bounds = new RectF(newX, newY, newW, newH);
        });
    }


    public static T FillAndPreserveAspectRatio<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
    {
        var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
        return DoParentTriggeredLayoutAction(child, parent, (c, p) =>
        {
            if (p.Width == 0 || p.Height == 0) return;

            var aspectRatio = (float)c.Width / c.Height;
            var newW = p.Width - (effectivePadding.Left + effectivePadding.Right);
            var newH = ConsoleMath.Round(newW / aspectRatio);

            if (newH > p.Height)
            {
                newH = p.Height;
                newW = ConsoleMath.Round(newH * aspectRatio);
            }

            var newLeft = (p.Width - newW) / 2;
            var newTop = (p.Height - newH) / 2;

            c.Bounds = new RectF(newLeft, newTop, newW, newH);
        });
    }

    public static T FillHorizontally<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
    {
        var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
        return DoParentTriggeredLayoutAction(child, parent, (c, p) =>
        {
            if (p.Width == 0) return;
            if (p.Width - (effectivePadding.Right + effectivePadding.Left) <= 0) return;
            c.Bounds = new RectF(effectivePadding.Left, c.Y, p.Width - (effectivePadding.Right + effectivePadding.Left), c.Height);
        });
    }

    public static T FillVertically<T>(this T child, Container parent = null, Thickness? padding = null) where T : ConsoleControl
    {
        var effectivePadding = padding.HasValue ? padding.Value : new Thickness(0, 0, 0, 0);
        return DoParentTriggeredLayoutAction(child, parent, (c, p) => c.Bounds = new RectF(c.X, effectivePadding.Top, c.Width, p.Height - (effectivePadding.Top + effectivePadding.Bottom)));
    }

    public static T DockToBottom<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl =>
        DoTwoWayLayoutAction(child, parent, (c, p) => c.Y = p.Height - c.Height - padding);


    public static T DockToTop<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl =>
        DoTwoWayLayoutAction(child, parent, (c, p) => c.Y = padding);


    public static T DockToRight<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl =>
        DoTwoWayLayoutAction(child, parent, (c, p) => c.X = p.Width - c.Width - padding);


    public static T DockToLeft<T>(this T child, Container parent = null, int padding = 0) where T : ConsoleControl =>
        DoTwoWayLayoutAction(child, parent, (c, p) => c.X = padding);


    private static T DoTwoWayLayoutAction<T>(this T child, Container parent, Action<T, Container> a) where T : ConsoleControl
    {
        parent = parent ?? child.Parent;
        var syncAction = () => a(child, parent);
        child.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
        parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
        syncAction();
        return child;
    }

    private static T DoParentTriggeredLayoutAction<T>(this T child, Container parent, Action<T, Container> a) where T : ConsoleControl
    {
        parent = parent ?? child.Parent;
        var syncAction = () => a(child, parent);
        parent.SubscribeForLifetime(nameof(ConsoleControl.Bounds), syncAction, parent);
        syncAction();
        return child;
    }
}