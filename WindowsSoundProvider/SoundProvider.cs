using ConsoleGames;
using PowerArgs;
using PowerArgs.Cli;
using System;

namespace WindowsSoundProvider
{
    public class SoundProvider : Disposable, ISoundProvider
    {
        private SoundThread SoundThread { get; set; }
        public SoundProvider() { SoundThread = new SoundThread(); SoundThread.Start(); }
        public Promise<Lifetime> Play(string name) => SoundThread.Play(name);
        public Promise<IDisposable> Loop(string name) => SoundThread.Loop(name);
        protected override void DisposeManagedResources() => SoundThread.Stop();
    }
}