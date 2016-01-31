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
   
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            Subscribe(nameof(Bounds), RedoLayout);
            Subscribe(nameof(Margin), RedoLayout);
            Controls.Added.Subscribe(Controls_Added);
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
