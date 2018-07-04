using ConsoleGames;
using PowerArgs;
using PowerArgs.Cli;
using System;

namespace WindowsSoundProvider
{
    public class SoundProvider : Disposable, ISoundProvider
    {
        public Promise StartPromise => SoundThread.StartPromise;
        private SoundThread SoundThread { get; set; }
        public SoundProvider() { SoundThread = new SoundThread(); SoundThread.Start(); }
        public Promise<Lifetime> Play(string name) => SoundThread.Play(name);
        public Promise<IDisposable> Loop(string name) => SoundThread.Loop(name);
        protected override void DisposeManagedResources() => SoundThread.Stop();
    }
}