using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public enum CompositionMode
    {
        PaintOver = 0,
        BlendBackground = 1,
        BlendVisible = 2,
    }

    /// <summary>
    /// A console control that has nested control within its bounds
    /// </summary>
    public class ConsolePanel : Container
    {
        /// <summary>
        /// The nested controls
        /// </summary>
        public ObservableCollection<ConsoleControl> Controls { get; private set; }

        private List<ConsoleControl> sortedControls = new List<ConsoleControl>();

        /// <summary>
        /// All nested controls, including those that are recursively nested within inner console panels
        /// </summary>
        public override IEnumerable<ConsoleControl> Children =>  Controls;

        public ConsolePanel() : this(1,1) { }

        /// <summary>
        /// Creates a new console panel
        /// </summary>
        public ConsolePanel(int w, int h) : base(w,h)
        {
            Controls = new ObservableCollection<ConsoleControl>();
            Controls.Added.SubscribeForLifetime((c) => 
            {
                c.Parent = this;
                sortedControls.Add(c);
                SortZ();
                c.SubscribeForLifetime(nameof(c.ZIndex), () => SortZ(), Controls.GetMembershipLifetime(c));
            }, this);
            Controls.AssignedToIndex.SubscribeForLifetime((assignment) => throw new NotSupportedException("Index assignment is not supported in Controls collection"), this);
            Controls.Removed.SubscribeForLifetime((c) => 
            {
                sortedControls.Remove(c);
                c.Parent = null; 
            }, this);

            this.OnDisposed(() =>
            {
                foreach(var child in Controls.ToArray())
                {
                    child.TryDispose();
                }
            });

            this.CanFocus = false;
        }

        /// <summary>
        /// Adds a control to the panel
        /// </summary>
        /// <typeparam name="T">the type of controls being added</typeparam>
        /// <param name="c">the control to add</param>
        /// <returns>the control that was added</returns>
        public T Add<T>(T c) where T : ConsoleControl
        {
            Controls.Add(c);
            return c;
        }

        /// <summary>
        /// Adds a collection of controls to the panel
        /// </summary>
        /// <param name="controls">the controls to add</param>
        public void AddRange(IEnumerable<ConsoleControl> controls)
        {
            foreach(var c in controls)
            {
                Controls.Add(c);
            }
        }

 

        private static Comparison<ConsoleControl> CompareZ = new Comparison<ConsoleControl>((a, b) =>
        {
            return a.ZIndex == b.ZIndex ? a.ParentIndex.CompareTo(b.ParentIndex) : a.ZIndex.CompareTo(b.ZIndex);
        });

        private void SortZ()
        {
            for (var i = 0; i < sortedControls.Count; i++)
            {
                sortedControls[i].ParentIndex = i;
            }
            sortedControls.Sort(CompareZ);
            ConsoleApp.Current?.RequestPaint();
        }
 
        /// <summary>
        /// Paints this control
        /// </summary>
        /// <param name="context">the drawing surface</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            foreach (var control in sortedControls)
            {
                if (control.Width > 0 && control.Height > 0 && control.IsVisible)
                {
                    Compose(control);
                }
            }

            foreach (var filter in RenderFilters)
            {
                filter.Control = this;
                filter.Filter(Bitmap);
            }
        }   
    }

    /// <summary>
    /// A ConsolePanel that can prevent outside influences from
    /// adding to its Controls collection. You must use the internal
    /// Unlock method to add or remove controls.
    /// </summary>
    public class ProtectedConsolePanel : Container
    {
        protected ConsolePanel ProtectedPanel { get; private set; }
        internal ConsolePanel ProtectedPanelInternal => ProtectedPanel;
        public override IEnumerable<ConsoleControl> Children =>  ProtectedPanel.Children;    

        /// <summary>
        /// Creates a new ConsolePanel
        /// </summary>
        public ProtectedConsolePanel()
        {
            this.CanFocus = false;
            ProtectedPanel = new ConsolePanel();
            ProtectedPanel.Parent = this;
            ProtectedPanel.Fill();
            this.SubscribeForLifetime(nameof(Background), () => ProtectedPanel.Background = Background, this);
            this.SubscribeForLifetime(nameof(Foreground), () => ProtectedPanel.Foreground = Foreground, this);
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            Compose(ProtectedPanel);
        }
    }
}
