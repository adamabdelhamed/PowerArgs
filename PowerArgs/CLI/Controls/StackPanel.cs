namespace PowerArgs.Cli
{
    public enum Orientation
    {
        Vertical,
        Horizontal,
    }

    public class StackPanel : ConsolePanel
    {
        public Orientation Orientation { get { return Get<Orientation>(); } set { Set(value); } }

        public int Margin{ get { return Get<int>(); } set { Set(value); } }

        public bool AutoSize { get; set; }

        public StackPanel()
        {
            SubscribeForLifetime(nameof(Bounds), RedoLayout, this.LifetimeManager);
            SubscribeForLifetime(nameof(Margin), RedoLayout, this.LifetimeManager);
            Controls.Added.SubscribeForLifetime(Controls_Added, this.LifetimeManager);
        }

        private void Controls_Added(ConsoleControl obj)
        {
            obj.SynchronizeForLifetime(nameof(Bounds), RedoLayout, obj.LifetimeManager);
        }

        private void RedoLayout()
        {
            if(Orientation == Orientation.Vertical)
            {
                int h = Layout.StackVertically(Margin, Controls);
                if(AutoSize)
                {
                    Height = h;
                }
            }
            else
            {
                int w = Layout.StackHorizontally(Margin, Controls);
                if(AutoSize)
                {
                    Width = w;
                }
            }
        }
    }
}
