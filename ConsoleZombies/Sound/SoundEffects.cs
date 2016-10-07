using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class SoundEffects
    {
        public SoundThread SoundThread { get; private set; }

        private static Lazy<SoundEffects> _instance = new Lazy<SoundEffects>(() => new SoundEffects(), true);
        public static SoundEffects Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private SoundEffects()
        {
            if(Scene.Current == null)
            {
                throw new InvalidOperationException("No render loop on current thread");
            }
            SoundThread = new SoundThread(Scene.Current);
        }

  

        public void PlaySound(string name)
        {
            SoundThread.Play(name, name == "music");
        }
    }
}
