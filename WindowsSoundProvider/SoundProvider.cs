using PowerArgs.Games;
using PowerArgs;
using PowerArgs.Cli;
using System;

namespace WindowsSoundProvider
{
    public class SoundProvider : Disposable, ISoundProvider
    {
        public Promise StartPromise => SoundThread.StartPromise;
        private SoundThread SoundThread { get; set; }
        public bool IsReady => SoundThread.IsReady;
        public SoundProvider() { SoundThread = new SoundThread(); SoundThread.Start(); }
        public Promise<Lifetime> Play(string name, float volume) => SoundThread.Play(name, volume);
        public Promise<IDisposable> Loop(string name, float volume) => SoundThread.Loop(name, volume);
        protected override void DisposeManagedResources() => SoundThread.Stop();
    }
}