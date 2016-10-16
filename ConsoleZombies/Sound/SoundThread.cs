using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ConsoleZombies
{
    public class SoundAction
    {
        public Action ToRun { get; set; }
    }

    public class StopSoundThreadAction : SoundAction
    {

    }
    

    public class SoundThread : Lifetime
    {
        public List<SoundPlaybackLifetime> CurrentlyPlayingSounds { get; private set; }

        private Dictionary<string, MediaPlayer> players;
        private Queue<SoundAction> soundQueue;
        private Thread theThread;
        private Scene scene;
        private object sync;

        private string soundsDir;

        public Scene Scene
        {
            get
            {
                return scene;
            }
        }

        public SoundThread(Scene scene, string soundsDir = @"C:\sfx")
        {
            this.soundsDir = soundsDir;
            this.scene = scene;
            sync = new object();
            soundQueue = new Queue<SoundAction>();
            CurrentlyPlayingSounds = new List<SoundPlaybackLifetime>();
        }

        public void Start()
        {
            lock (sync)
            {
                if (theThread != null) return;

                theThread = new Thread(Run);
                theThread.SetApartmentState(ApartmentState.STA);
                theThread.IsBackground = true;
                theThread.Start();
            }
        }

        public void Stop()
        {
            lock (sync)
            {
                if (theThread == null) return;
                lock(soundQueue)
                {
                    soundQueue.Enqueue(new StopSoundThreadAction());
                }
                theThread = null;
            }
        }

        public void Play(string name, bool loop)
        {
            if (HasSound(name) == false) return;
            EnqueueSoundThreadAction(()=>
            {
                var player = players[name];
                players[name] = PreLoad(name);
                CurrentlyPlayingSounds.Add(new SoundPlaybackLifetime(player, loop, this));
            });
        }

        public void EnqueueSoundThreadAction(Action soundPlayingAction)
        {
            lock (soundQueue)
            {
                soundQueue.Enqueue(new SoundAction() { ToRun = soundPlayingAction });
            }
        }

        private void Run()
        {
            players = LoadSounds();

            Window hiddenWindow = new Window()
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false
            };
            hiddenWindow.Visibility = Visibility.Hidden;
            hiddenWindow.Loaded += (s, o) =>
            {
                Thread.CurrentThread.IsBackground = true;
                DispatcherTimer t = new DispatcherTimer();
                t.Interval = TimeSpan.FromMilliseconds(1);
                t.Tick += (s1, o1) =>
                {
                    Queue<SoundAction> toRun = new Queue<SoundAction>();
                    lock (soundQueue)
                    {
                        while (soundQueue.Count > 0)
                        {
                            toRun.Enqueue(soundQueue.Dequeue());
                        }
                    }

                    while (toRun.Count > 0)
                    {
                        var next = toRun.Dequeue();
                        if (next is StopSoundThreadAction)
                        {
                            t.Stop();
                            CurrentlyPlayingSounds.ForEach(sound => sound.Player.Stop());
                            hiddenWindow.Close();
                        }
                        else
                        {
                            next.ToRun.Invoke();
                        }
                    }
                };
                t.Start();
            };
            hiddenWindow.Show();
            hiddenWindow.Visibility = Visibility.Hidden;
            Dispatcher.Run();
        }

        private Dictionary<string, MediaPlayer> LoadSounds()
        {
            var ret = new Dictionary<string, MediaPlayer>();

            foreach(var file in Directory.GetFiles(soundsDir))
            {
                var key = Path.GetFileNameWithoutExtension(file);
                ret.Add(key, PreLoad(key));
            }

            return ret;
        }

        private bool HasSound(string name)
        {
            var file = Path.Combine(soundsDir, name + ".wav");

            if (File.Exists(file) == false)
            {
                file = Path.Combine(soundsDir, name + ".m4a");

                if(File.Exists(file))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                return true;
            }
        }

        private MediaPlayer PreLoad(string name)
        {
            var file = Path.Combine(soundsDir, name + ".wav");

            if (File.Exists(file) == false)
            {
                file = Path.Combine(soundsDir, name + ".m4a");
            }

            MediaPlayer player = new MediaPlayer();
            if(name == "music")
            {
                player.Volume = .1;
            }
            player.Open(new Uri(file));
            return player;
        }
    }
}
