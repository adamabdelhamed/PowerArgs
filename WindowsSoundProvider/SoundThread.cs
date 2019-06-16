using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using PowerArgs;
using System.Reflection;

namespace WindowsSoundProvider
{
    internal class SoundAction
    {
        public Action ToRun { get; set; }
    }

    internal class StopSoundThreadAction : SoundAction
    {

    }


    internal class SoundThread : Lifetime
    {
        private Deferred startDeferred = Deferred.Create();
        public Promise StartPromise => startDeferred.Promise;
 
        [ThreadStatic]
        private static SoundThread _current;

        public static SoundThread Current
        {
            get
            {
                return _current;
            }
        }

        public static void AssertSoundThread()
        {
            if (_current == null)
            {
                throw new InvalidOperationException("No sound thread");
            }
        }


        public List<SoundPlaybackLifetime> CurrentlyPlayingSounds { get; private set; }

        private Dictionary<string, MediaPlayer> players;
        private Queue<SoundAction> soundQueue;
        private Thread theThread;
        private object sync;

        private string soundsDir;

        public SoundThread(string soundsDir = null)
        {
            soundsDir = soundsDir ?? Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),"sfx");
            this.soundsDir = soundsDir;

            if(Directory.Exists(soundsDir) == false)
            {
                Directory.CreateDirectory(soundsDir);
            }

            sync = new object();
            soundQueue = new Queue<SoundAction>();
            CurrentlyPlayingSounds = new List<SoundPlaybackLifetime>();
        }

        public void Start()
        {
            lock (sync)
            {
                if (theThread != null) return;

                theThread = new Thread(Run) { Name = "SoundThread" };
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
                lock (soundQueue)
                {
                    soundQueue.Enqueue(new StopSoundThreadAction());
                }
                theThread = null;
            }
        }

        public Promise<Lifetime> Play(string name, float volume)
        {
            var d = Deferred<Lifetime>.Create();
            if (players.ContainsKey(name))
            {
                EnqueueSoundThreadAction(() =>
                {
                    var player = players[name];
                    players[name] = PreLoad(name);
                    var soundLifetime = new SoundPlaybackLifetime(player, false, this, volume);
                    CurrentlyPlayingSounds.Add(soundLifetime);
                    d.Resolve(soundLifetime);
                });
            }
            else
            {
                var lifetime = new Lifetime();
                lifetime.Dispose();
                d.Resolve(lifetime);
            }

            return d.Promise;
        }

        public Promise<IDisposable> Loop(string name, float volume)
        {
            var d = Deferred<IDisposable>.Create();
            if (players.ContainsKey(name))
            {
                EnqueueSoundThreadAction(() =>
                {
                    var player = players[name];
                    players[name] = PreLoad(name);
                    var soundLifetime = new SoundPlaybackLifetime(player, true, this, volume);
                    CurrentlyPlayingSounds.Add(soundLifetime);
                    d.Resolve(soundLifetime);
                });
            }
            else
            {
                var lifetime = new Lifetime();
                lifetime.Dispose();
                d.Resolve(lifetime);
            }

            return d.Promise;
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
                _current = this;
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
                            CurrentlyPlayingSounds.ToArray().ToList().ForEach(sound => sound.Dispose());
                            hiddenWindow.Close();
                            _current = null;
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
            startDeferred.Resolve();
            Dispatcher.Run();
        }

        private Dictionary<string, MediaPlayer> LoadSounds()
        {
            var ret = new Dictionary<string, MediaPlayer>();

            foreach (var file in Directory.GetFiles(soundsDir).Select(f => f.ToLower()))
            {
                if (file.ToLower().EndsWith(".wav") || file.ToLower().EndsWith(".mp3") || file.ToLower().EndsWith(".m4a"))
                {
                    var key = Path.GetFileNameWithoutExtension(file);
                    ret.Add(key, PreLoad(key));
                }
            }

            return ret;
        }
 

        private MediaPlayer PreLoad(string name)
        {
            var file = Path.Combine(soundsDir, name + ".wav");

            if (File.Exists(file) == false)
            {
                file = Path.Combine(soundsDir, name + ".m4a");
            }

            if (File.Exists(file) == false)
            {
                file = Path.Combine(soundsDir, name + ".mp3");
            }

            MediaPlayer player = new MediaPlayer();
            player.Volume = 0;
            player.Open(new Uri(file));
            return player;
        }
    }
}