using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public enum PlayerState
    {
        NotLoaded,
        Failed,
        Playing,
        Buffering,
        Paused,
        Stopped,
    }

    public class ConsoleBitmapPlayer : ConsolePanel
    {
        public PlayerState State { get { return Get<PlayerState>(); } set { Set(value); } }

        public TimeSpan RewindAndFastForwardIncrement { get { return Get<TimeSpan>(); } set { Set(value); } }

        private string ErrorMessage;
        private PlayerProgressBar progressBar;
        private Border pictureFrame;
        private BitmapControl pictureInTheFrame;
        private Button playButton, seekToBeginningButton, seekBack10SButton, seekForward10SButton, seekToEndButton;
        private Lifetime playLifetime;
        private TimeSpan duration;


        private TimeSpan playStartPosition;
        private DateTime playStartTime;


        private ConsoleBitmap CurrentFrame
        {
            get
            {
                return pictureInTheFrame.Bitmap;
            }
            set
            {
                pictureInTheFrame.Bitmap = value;
            }
        }

        private InMemoryConsoleBitmapVideo inMemoryVideo;

        public ConsoleBitmapPlayer()
        {
            this.CanFocus = false;
            RewindAndFastForwardIncrement = TimeSpan.FromSeconds(10);
            pictureFrame = Add(new Border()).Fill(padding: new Thickness(0,0,0,1));
            pictureFrame.Background = ConsoleColor.DarkGray;
            pictureInTheFrame = pictureFrame.Add(new BitmapControl() { AutoSize = true, CanFocus = false }).CenterHorizontally().CenterVertically();
            progressBar = Add(new PlayerProgressBar() { ShowPlayCursor = false }).FillHoriontally(padding: new Thickness(0,0,0,0)).DockToBottom(padding: 1);

            var buttonBar = Add(new StackPanel() { CanFocus =false, Height=1, Orientation = Orientation.Horizontal }).FillHoriontally().DockToBottom();

            seekToBeginningButton = buttonBar.Add(new Button() { Text = "<<".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.Home), CanFocus = false });
            seekToBeginningButton.Pressed.SubscribeForLifetime(SeekToBeginningButtonPressed, this.LifetimeManager);

            seekBack10SButton = buttonBar.Add(new Button() { Shortcut = new KeyboardShortcut(ConsoleKey.LeftArrow), CanFocus = false });
            seekBack10SButton.Pressed.SubscribeForLifetime(Rewind, this.LifetimeManager);

            playButton = buttonBar.Add(new Button() { Text = "".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.P), CanFocus = false });
            playButton.Pressed.SubscribeForLifetime(PlayPressed, this.LifetimeManager);

            seekForward10SButton = buttonBar.Add(new Button() { Shortcut = new KeyboardShortcut(ConsoleKey.RightArrow), CanFocus = false });
            seekForward10SButton.Pressed.SubscribeForLifetime(FastForward, this.LifetimeManager);

            seekToEndButton = buttonBar.Add(new Button() { Text = ">>".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.End), CanFocus = false });
            seekToEndButton.Pressed.SubscribeForLifetime(SeekToEndButtonPressed, this.LifetimeManager);

            this.SubscribeForLifetime(nameof(State), StateChanged, this.LifetimeManager);

            this.SynchronizeForLifetime(nameof(RewindAndFastForwardIncrement), () =>
            {
                seekBack10SButton.Text = $"< {RewindAndFastForwardIncrement.TotalSeconds}s".ToConsoleString();
                seekForward10SButton.Text = $"{RewindAndFastForwardIncrement.TotalSeconds}s >".ToConsoleString();
            }, this.LifetimeManager);

            State = PlayerState.NotLoaded;
        }

        private void SeekToBeginningButtonPressed()
        {
            playStartPosition = TimeSpan.Zero;
            playStartTime = DateTime.UtcNow;
            progressBar.PlayCursorPosition = 0;
            if (inMemoryVideo != null && inMemoryVideo.Frames.Count > 0)
            {
                CurrentFrame = inMemoryVideo.Frames[0].Bitmap;
            }
        }

        private void SeekToEndButtonPressed()
        {
            playStartPosition = duration;
            playStartTime = DateTime.UtcNow;
            progressBar.PlayCursorPosition = Math.Min(1, progressBar.LoadProgressPosition);
            if (inMemoryVideo != null && inMemoryVideo.Frames.Count > 0)
            {
                CurrentFrame = inMemoryVideo.Frames[inMemoryVideo.Frames.Count-1].Bitmap;
            }
        }

        private void Rewind()
        {
            var numSecondsBack = RewindAndFastForwardIncrement.TotalSeconds;
            var tenSecondsPercentage = numSecondsBack / duration.TotalSeconds;
            if (tenSecondsPercentage > 1) tenSecondsPercentage = 1;

            var newCursorPosition = progressBar.PlayCursorPosition - tenSecondsPercentage;
            if (newCursorPosition < 0) newCursorPosition = 0;
            progressBar.PlayCursorPosition = newCursorPosition;

            playStartPosition = TimeSpan.FromSeconds(progressBar.PlayCursorPosition * duration.TotalSeconds);
            playStartTime = DateTime.UtcNow;
        }

        private void FastForward()
        {
            var numSecondsForward = RewindAndFastForwardIncrement.TotalSeconds;
            var tenSecondsPercentage = numSecondsForward / duration.TotalSeconds;
            if (tenSecondsPercentage > 1) tenSecondsPercentage = 1;

            var newCursorPosition = progressBar.PlayCursorPosition + tenSecondsPercentage;
            if (newCursorPosition > 1) newCursorPosition = 1;
            progressBar.PlayCursorPosition = Math.Min(progressBar.LoadProgressPosition, newCursorPosition);

            playStartPosition = TimeSpan.FromSeconds(progressBar.PlayCursorPosition * duration.TotalSeconds);
            playStartTime = DateTime.UtcNow;
        }

        private void PlayPressed()
        {
            if (State == PlayerState.Playing)
            {
                State = PlayerState.Paused;
            }
            else if (State == PlayerState.Paused || State == PlayerState.Buffering)
            {
                State = PlayerState.Playing;
            }
            else if(State == PlayerState.Stopped)
            {
                if(progressBar.PlayCursorPosition == 1)
                {
                    progressBar.PlayCursorPosition = 0;
                }

                State = PlayerState.Playing;
            }
        }


        private void StateChanged()
        {
            if(State == PlayerState.Playing)
            {
                if (CurrentFrame == null) throw new InvalidOperationException("No video loaded, can't play");

                playStartPosition = TimeSpan.FromSeconds(progressBar.PlayCursorPosition * duration.TotalSeconds);
                playStartTime = DateTime.UtcNow;

                // start a play loop for as long as the state remains unchanged
                this.playLifetime = this.GetPropertyValueLifetime(nameof(State));
                playLifetime.LifetimeManager.Manage(Application.SetInterval(() =>
                {
                    if(State != PlayerState.Playing)
                    {
                        return;
                    }

                    var now = DateTime.UtcNow;
                    var delta = now - playStartTime;
                    var newPlayerPosition = playStartPosition + delta;
                    var videoLocationPercentage = Math.Round(100.0 *newPlayerPosition.TotalSeconds / duration.TotalSeconds,1);
                    videoLocationPercentage = Math.Min(videoLocationPercentage, 100);
                    progressBar.PlayCursorPosition = videoLocationPercentage / 100.0;
                    playButton.Text = $"Pause".ToConsoleString();

                    ConsoleBitmap seekedImage;
                    if (newPlayerPosition > duration)
                    {
                        State = PlayerState.Stopped;
                    }
                    else if(inMemoryVideo.TrySeek(newPlayerPosition, out seekedImage) == false)
                    {
                        State = PlayerState.Buffering;
                    }
                    else
                    {
                        CurrentFrame = seekedImage;
                    }

                }, TimeSpan.FromMilliseconds(1)));
            }
            else if(State == PlayerState.Stopped)
            {
                playButton.Text = "Play".ToConsoleString();
            }
            else if (State == PlayerState.Paused)
            {
                playButton.Text = "Play".ToConsoleString();
            }
            else if(State == PlayerState.NotLoaded)
            {
                playButton.Text = "Play".ToConsoleString();
                playButton.CanFocus = false;
            }
            else if (State == PlayerState.Buffering)
            {
                playButton.Text = "Play".ToConsoleString();
            }
            else
            {
                throw new Exception("Unknown state: "+State);
            }
        }
        
        public void Load(Stream videoStream)
        {
            if(Application == null)
            {
                throw new InvalidOperationException("Can't load until the control has been added to an application");
            }

            Task.Factory.StartNew(() =>
            {
                try
                {
                    var reader = new ConsoleBitmapStreamReader(videoStream);
                    reader.ReadToEnd((videoWithProgressInfo) =>
                    {
                        inMemoryVideo = inMemoryVideo ?? videoWithProgressInfo;
                        this.duration = videoWithProgressInfo.Duration;
                        Application.QueueAction(() => 
                        {
                            if (this.CurrentFrame == null)
                            {
                                this.CurrentFrame = videoWithProgressInfo.Frames[0].Bitmap;
                                progressBar.ShowPlayCursor = true;
                                playButton.CanFocus = true;
                                seekToBeginningButton.CanFocus = true;
                                seekBack10SButton.CanFocus = true;
                                seekForward10SButton.CanFocus = true;
                                seekToEndButton.CanFocus = true;
                                State = PlayerState.Stopped;
                                if(Application.FocusManager.FocusedControl == null)
                                {
                                    Application.FocusManager.TrySetFocus(playButton);
                                }
                            }

                            progressBar.LoadProgressPosition = inMemoryVideo.LoadProgress;
                        });
                        Thread.Sleep(1000);
                    });
                }
                catch (Exception ex)
                {
                    Application.QueueAction(() => 
                    {
                        State = PlayerState.Failed;
                        pictureFrame.BorderColor = ConsoleColor.Red;
                    });
                }
            });
        }
    }
}
