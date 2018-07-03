using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Represents the orientation of a 2d visual
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// Vertical orientation (up and down)
        /// </summary>
        Vertical,
        /// <summary>
        /// Horizontal orientation (left and right)
        /// </summary>
        Horizontal,
    }

    /// <summary>
    /// A panel that handles stacking child controls
    /// </summary>
    public class StackPanel : ConsolePanel
    {
        /// <summary>
        /// Gets or sets the orientation of the control
        /// </summary>
        public Orientation Orientation { get { return Get<Orientation>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets the value, in number of console pixels to space between child elements.  Defaults to 0.
        /// </summary>
        public int Margin{ get { return Get<int>(); } set { Set(value); } }

        /// <summary>
        /// When set to true, the panel will size itself automatically based on its children.
        /// </summary>
        public bool AutoSize { get; set; }

        /// <summary>
        /// Creates a new stack panel
        /// </summary>
        public StackPanel()
        {
            SubscribeForLifetime(nameof(Bounds), RedoLayout, this);
            SubscribeForLifetime(nameof(Margin), RedoLayout, this);
            Controls.Added.SubscribeForLifetime(Controls_Added, this);
        }

        private void Controls_Added(ConsoleControl obj)
        {
            obj.SynchronizeForLifetime(nameof(Bounds), RedoLayout, Controls.GetMembershipLifetime(obj));
        }

        private void RedoLayout()
        {
            if(Orientation == Orientation.Vertical)
            {
                int h = Layout.StackVertically(Margin, Controls);
                if(AutoSize)
                {
                    Height = h;
                    Width = Controls.Select(c => c.X + c.Width).Max();
                }
            }
            else
            {
                int w = Layout.StackHorizontally(Margin, Controls);
                if(AutoSize)
                {
                    Width = w;
                    Height = Controls.Select(c => c.Y + c.Height).Max();
                }
            }
        }
    }
}
