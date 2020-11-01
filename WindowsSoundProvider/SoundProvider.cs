using PowerArgs.Games;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Threading.Tasks;

namespace WindowsSoundProvider
{
    public class SoundProvider : Disposable, ISoundProvider
    {
        public Task StartTask => SoundThread.StartTask;
        private SoundThread SoundThread { get; set; }
        public bool IsReady => SoundThread.IsReady;
        public SoundProvider() { SoundThread = new SoundThread(); SoundThread.Start(); }
        public Task<Lifetime> Play(string name, float volume) => SoundThread.Play(name, volume);
        public Task<IDisposable> Loop(string name, float volume) => SoundThread.Loop(name, volume);
        protected override void DisposeManagedResources() => SoundThread.Stop();
    }
}