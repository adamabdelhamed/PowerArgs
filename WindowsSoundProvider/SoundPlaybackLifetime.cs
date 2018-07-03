using PowerArgs.Cli;
using System;
using System.Windows.Media;

namespace WindowsSoundProvider
{
    internal class SoundPlaybackLifetime : Lifetime
    {
        public MediaPlayer Player { get; private set; }
        private bool loop;
        private SoundThread soundThread;
        public SoundPlaybackLifetime(MediaPlayer player, bool loop, SoundThread soundThread)
        {
            this.Player = player;
            this.loop = loop;
            this.soundThread = soundThread;

            player.MediaEnded += Player_MediaEnded;
            player.Play();

            /*

            soundThread.Scene.SubscribeForLifetime(nameof(Scene.SpeedFactor), () =>
            {
                soundThread.EnqueueSoundThreadAction(() =>
                {
                    player.SpeedRatio = soundThread.Scene.SpeedFactor == 1 ? 1 : .4;
                });

            }, this.LifetimeManager);
            */
        }

        private void Player_MediaEnded(object sender, EventArgs e)
        {
            lock (Player)
            {
                if (IsExpired)
                {
                    return;
                }

                if (loop)
                {
                    Player.Position = TimeSpan.Zero;
                    Player.Play();
                }
                else
                {
                    Dispose();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            lock (Player)
            {

                SoundThread.AssertSoundThread();
                Player.Stop();

                lock (soundThread.CurrentlyPlayingSounds)
                {
                    soundThread.CurrentlyPlayingSounds.Remove(this);
                }
            }
        }
    }
}