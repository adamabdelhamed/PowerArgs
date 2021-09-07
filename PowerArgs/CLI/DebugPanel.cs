namespace PowerArgs.Cli
{
    internal class DebugPanel : LogTailControl
    {
        public static readonly RGB ForegroundColor = RGB.Black;
        public static readonly RGB BackgroundColor = RGB.DarkYellow;

        public DebugPanel()
        {
            this.Foreground = ForegroundColor;
            this.Background = BackgroundColor;
            this.Ready.SubscribeOnce(() =>
            {
                Application.ConsoleOutTextReady
                .SubscribeForLifetime(s => Append(s), this);
            });
        }
    }
}
