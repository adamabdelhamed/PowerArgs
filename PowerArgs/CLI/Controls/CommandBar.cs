using System;

namespace PowerArgs.Cli
{
    public class CommandBar : ConsolePanel
    {
        public CommandBar()
        {
            this.Height = 1;
            this.Controls.SynchronizeForLifetime(Commands_Added, Commands_Removed, () => { }, this);
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
