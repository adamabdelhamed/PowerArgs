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
            Controls.Added += Controls_Added;
            Controls.Removed += Controls_Removed;
            Subscribe(nameof(Bounds), RedoLayout);
            Subscribe(nameof(Margin), RedoLayout);
        }

        private void Controls_Added(ConsoleControl obj)
        {
            obj.PropertyChanged += Obj_PropertyChanged;
            RedoLayout();
        }

        private void Controls_Removed(ConsoleControl obj)
        {
            obj.PropertyChanged -= Obj_PropertyChanged;
            RedoLayout();
        }

        private void Obj_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Bounds))
            {
                RedoLayout();
            }
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
