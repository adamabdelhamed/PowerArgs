using System;

namespace PowerArgs.Cli
{
    public class CommandBar : ConsolePanel
    {
        public CommandBar()
        {
            this.Height = 1;
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            this.Controls.Synchronize(Commands_Added, Commands_Removed, ()=> { });
        }

        private void Commands_Added(ConsoleControl c)
        {
            Layout.StackHorizontally(1, Controls);
        }

        private void Commands_Removed(ConsoleControl c)
        {
            Layout.StackHorizontally(1, Controls);
        }
    }
}
