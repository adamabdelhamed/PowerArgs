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
        public void Play(string name) => SoundThread.Play(name, false);
        public Promise<IDisposable> Loop(string name) => SoundThread.Play(name, true);
        protected override void DisposeManagedResources() => SoundThread.Stop();
    }
}