using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Windows.Media;

namespace ConsoleZombies
{
    public class SoundPlaybackLifetime : Lifetime
    {
        private MediaPlayer player;
        private bool loop;
        private SoundThread soundThread;
        public SoundPlaybackLifetime(MediaPlayer player, bool loop, SoundThread soundThread)
        {
            this.player = player;
            this.loop = loop;
            this.soundThread = soundThread;

            player.MediaEnded += Player_MediaEnded;
            player.Play();


            soundThread.Scene.SubscribeForLifetime(nameof(Scene.SpeedFactor), () =>
            {
                soundThread.EnqueueSoundThreadAction(() =>
                {
                    player.SpeedRatio = soundThread.Scene.SpeedFactor == 1 ? 1 : .4;
                });

            }, this.LifetimeManager);
        }

        private void Player_MediaEnded(object sender, EventArgs e)
        {
            lock (player)
            {
                if (IsExpired)
                {
                    return;
                }

                if (loop)
                {
                    player.Position = TimeSpan.Zero;
                    player.Play();
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
            lock (player)
            {
                soundThread.EnqueueSoundThreadAction(() =>
                {
                    player.Stop();
                    lock (soundThread.CurrentlyPlayingSounds)
                    {
                        soundThread.CurrentlyPlayingSounds.Remove(this);
                    }
                });
            }
        }
    }
}
