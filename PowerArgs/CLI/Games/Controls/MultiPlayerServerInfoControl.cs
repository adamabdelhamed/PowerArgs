using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class MultiPlayerServerInfoControl : ConsolePanel
    {
        private MultiPlayerServer server;
        private LogTailControl tail;
        public MultiPlayerServerInfoControl(MultiPlayerServer server)
        {
            this.server = server;
            tail = Add(new LogTailControl()).Fill();
            server.Error.SubscribeForLifetime((msg)   => Application?.QueueAction(() => OnError(msg)), this);
            server.Warning.SubscribeForLifetime((msg) => Application?.QueueAction(() => OnWarning(msg)), this);
            server.Info.SubscribeForLifetime((msg)    => Application?.QueueAction(() => OnInfo(msg)), this);
        }

        private void OnError(string msg) => tail.Append(msg.ToRed());
        private void OnWarning(string msg) => tail.Append(msg.ToYellow());
        private void OnInfo(string msg) => tail.Append(msg.ToCyan());
    }
}