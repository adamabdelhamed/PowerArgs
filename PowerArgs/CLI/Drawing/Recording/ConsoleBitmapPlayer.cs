using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// All states that the player can be in
    /// </summary>
    public enum PlayerState
    {
        /// <summary>
        /// The initial state when there is no video loaded
        /// </summary>
        NotLoaded,
        /// <summary>
        /// A video failed to load
        /// </summary>
        Failed,
        /// <summary>
        /// A video is playing
        /// </summary>
        Playing,
        /// <summary>
        /// A video is buffering
        /// </summary>
        Buffering,
        /// <summary>
        /// A video is paused
        /// </summary>
        Paused,
        /// <summary>
        ///  A video is stopped
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// A control that can play console app recordings from a stream
    /// </summary>
    public class ConsoleBitmapPlayer : ConsolePanel
    {
        /// <summary>
        /// Gets the current state of the player
        /// </summary>
        public PlayerState State { get { return Get<PlayerState>(); } private set { Set(value); } }

        /// <summary>
        /// An artificial delay that is added after each frame is loaded from the stream.  This can simulate
        /// a slow loading connection and is good for testing.  This should always be set to null when PowerArgs ships.
        /// </summary>
        internal TimeSpan? AfterFrameLoadDelay { get; set; } = null;

        /// <summary>
        /// Gets or sets the rewind and fast forward increment, defaults to 10 seconds
        /// </summary>
        public TimeSpan RewindAndFastForwardIncrement { get { return Get<TimeSpan>(); } set { Set(value); } }

        /// <summary>
        /// The bar that's rendered below the player.  It shows the current play cursor and loading progress.
        /// </summary>
        private PlayerProgressBar playerProgressBar;

        /// <summary>
        /// The border control that hosts the current frame inside of it
        /// </summary>
        private Border pictureFrame;

        /// <summary>
        /// The control that renders the current frame in the video
        /// </summary>
        private BitmapControl pictureInTheFrame;

        /// <summary>
        /// The buttons that appear under the player progress bar
        /// </summary>
        private Button playButton, seekToBeginningButton, seekBack10SButton, seekForward10SButton, seekToEndButton;

        /// <summary>
        /// The lifetime of the current play operation (or null if the player is not playing)
        /// </summary>
        private Lifetime playLifetime;

        /// <summary>
        /// The duration of the currently loaded video.  This is set once the first frame of the video is loaded
        /// </summary>
        private TimeSpan? duration;

        /// <summary>
        /// The in memory video data structure.  This is set once the first frame of the video is loaded
        /// </summary>
        private InMemoryConsoleBitmapVideo inMemoryVideo;

        /// <summary>
        /// The cursor position at the time playback started.  If the current state is not Playing then this value
        /// is meaningless.
        /// </summary>
        private TimeSpan playStartPosition;

        /// <summary>
        /// The wall clock time when playback started.  If the current state is not playing then this value is meaningless.
        /// </summary>
        private DateTime playStartTime;

        /// <summary>
        /// The most recent frame index received from the reader's TrySeek method
        /// </summary>
        private int lastFrameIndex;

        /// <summary>
        /// The error message to show if loading failed
        /// </summary>
        private string failedMessage;

        /// <summary>
        /// Gets or sets the current frame image
        /// </summary>
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


        /// <summary>
        /// Creates a console bitmap player control with no video loaded
        /// </summary>
        public ConsoleBitmapPlayer()
        {
            this.CanFocus = false;
            RewindAndFastForwardIncrement = TimeSpan.FromSeconds(10);
            pictureFrame = Add(new Border()).Fill(padding: new Thickness(0,0,0,1));
            pictureFrame.Background = ConsoleColor.DarkGray;
            pictureInTheFrame = pictureFrame.Add(new BitmapControl() { AutoSize = true, CanFocus = false }).CenterBoth();
            playerProgressBar = Add(new PlayerProgressBar() { ShowPlayCursor = false }).FillHorizontally(padding: new Thickness(0,0,0,0)).DockToBottom(padding: 1);

            var buttonBar = Add(new StackPanel() { CanFocus =false, Height=1, Orientation = Orientation.Horizontal }).FillHorizontally().DockToBottom();

            seekToBeginningButton = buttonBar.Add(new Button() { Text = "<<".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.Home), CanFocus = false });
            seekToBeginningButton.Pressed.SubscribeForLifetime(SeekToBeginningButtonPressed, this);

            seekBack10SButton = buttonBar.Add(new Button() { Shortcut = new KeyboardShortcut(ConsoleKey.LeftArrow), CanFocus = false });
            seekBack10SButton.Pressed.SubscribeForLifetime(Rewind, this);

            playButton = buttonBar.Add(new Button() { Text = "".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.P), CanFocus = false });
            playButton.Pressed.SubscribeForLifetime(PlayPressed, this);

            seekForward10SButton = buttonBar.Add(new Button() { Shortcut = new KeyboardShortcut(ConsoleKey.RightArrow), CanFocus = false });
            seekForward10SButton.Pressed.SubscribeForLifetime(FastForward, this);

            seekToEndButton = buttonBar.Add(new Button() { Text = ">>".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.End), CanFocus = false });
            seekToEndButton.Pressed.SubscribeForLifetime(SeekToEndButtonPressed, this);

            this.SubscribeForLifetime(nameof(State), StateChanged, this);

            this.SynchronizeForLifetime(nameof(RewindAndFastForwardIncrement), () =>
            {
                seekBack10SButton.Text = $"< {RewindAndFastForwardIncrement.TotalSeconds}s".ToConsoleString();
                seekForward10SButton.Text = $"{RewindAndFastForwardIncrement.TotalSeconds}s >".ToConsoleString();
            }, this);

            State = PlayerState.NotLoaded;
        }

        /// <summary>
        /// Seeks to the beginning of the video.  If the video is playing then it will continue playing
        /// from the beginning of the video
        /// </summary>
        private void SeekToBeginningButtonPressed()
        {
            if (duration.HasValue == false)
            {
                throw new InvalidOperationException("Seeking is not permitted before a video is loaded");
            }

            playStartPosition = TimeSpan.Zero;
            playStartTime = DateTime.UtcNow;
            playerProgressBar.PlayCursorPosition = 0;
            if (inMemoryVideo != null && inMemoryVideo.Frames.Count > 0)
            {
                CurrentFrame = inMemoryVideo.Frames[0].Bitmap;
            }
        }


        /// <summary>
        /// Seeks to the end of the video
        /// </summary>
        private void SeekToEndButtonPressed()
        {
            if(duration.HasValue == false)
            {
                throw new InvalidOperationException("Seeking is not permitted before a video is loaded");
            }

            playStartPosition = duration.Value;
            playStartTime = DateTime.UtcNow;
            playerProgressBar.PlayCursorPosition = Math.Min(1, playerProgressBar.LoadProgressPosition);
            if (inMemoryVideo != null && inMemoryVideo.Frames.Count > 0)
            {
                CurrentFrame = inMemoryVideo.Frames[inMemoryVideo.Frames.Count-1].Bitmap;
            }
        }

        /// <summary>
        /// Rewinds the video by the amount defined by the RewindAndFastForwardIncrement.  If the
        /// video was playing then it will continue to play
        /// </summary>
        private void Rewind()
        {
            if (duration.HasValue == false)
            {
                throw new InvalidOperationException("Rewind is not permitted before a video is loaded");
            }

            var numSecondsBack = RewindAndFastForwardIncrement.TotalSeconds;
            var tenSecondsPercentage = numSecondsBack / duration.Value.TotalSeconds;
            if (tenSecondsPercentage > 1) tenSecondsPercentage = 1;

            var newCursorPosition = playerProgressBar.PlayCursorPosition - tenSecondsPercentage;
            if (newCursorPosition < 0) newCursorPosition = 0;
            playerProgressBar.PlayCursorPosition = newCursorPosition;

            playStartPosition = TimeSpan.FromSeconds(playerProgressBar.PlayCursorPosition * duration.Value.TotalSeconds);
            playStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Fast forwards the video by the amount defined by the RewindAndFastForwardIncrement.  If the
        /// video was playing then it will continue to play, unless it hits the end of the video.
        /// </summary>
        private void FastForward()
        {
            if (duration.HasValue == false)
            {
                throw new InvalidOperationException("Fast forward is not permitted before a video is loaded");
            }

            var numSecondsForward = RewindAndFastForwardIncrement.TotalSeconds;
            var tenSecondsPercentage = numSecondsForward / duration.Value.TotalSeconds;
            if (tenSecondsPercentage > 1) tenSecondsPercentage = 1;

            var newCursorPosition = playerProgressBar.PlayCursorPosition + tenSecondsPercentage;
            if (newCursorPosition > 1) newCursorPosition = 1;
            playerProgressBar.PlayCursorPosition = Math.Min(playerProgressBar.LoadProgressPosition, newCursorPosition);

            playStartPosition = TimeSpan.FromSeconds(playerProgressBar.PlayCursorPosition * duration.Value.TotalSeconds);
            playStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// The handler for the play button that handles play / pause toggling and resetting to the beginning
        /// if the player is currently stopped at the end of the video.
        /// </summary>
        private void PlayPressed()
        {
            if (duration.HasValue == false)
            {
                throw new InvalidOperationException("Playback is not permitted before a video is loaded");
            }

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
                if(playerProgressBar.PlayCursorPosition == 1)
                {
                    playerProgressBar.PlayCursorPosition = 0;
                }

                State = PlayerState.Playing;
            }
        }

        /// <summary>
        /// The state change handler that defines what happens whenever the player changes state
        /// </summary>
        private void StateChanged()
        {
            if (State != PlayerState.Playing)
            {
                playLifetime = null;
            }

            if(State == PlayerState.Playing)
            {
                if(duration.HasValue == false)
                {
                    throw new InvalidOperationException("Playback is not permitted before a video is loaded");
                }

                playStartPosition = TimeSpan.FromSeconds(playerProgressBar.PlayCursorPosition * duration.Value.TotalSeconds);
                playStartTime = DateTime.UtcNow;
                lastFrameIndex = 0;
                // start a play loop for as long as the state remains unchanged
                this.playLifetime = this.GetPropertyValueLifetime(nameof(State));
                playLifetime.OnDisposed(Application.SetInterval(() =>
                {
                    if(State != PlayerState.Playing)
                    {
                        return;
                    }
                    var now = DateTime.UtcNow;
                    var delta = now - playStartTime;
                    var newPlayerPosition = playStartPosition + delta;
                    var videoLocationPercentage = Math.Round(100.0 *newPlayerPosition.TotalSeconds / duration.Value.TotalSeconds,1);
                    videoLocationPercentage = Math.Min(videoLocationPercentage, 100);
                    playerProgressBar.PlayCursorPosition = videoLocationPercentage / 100.0;
                    playButton.Text = $"Pause".ToConsoleString();

                    ConsoleBitmap seekedImage;
 
                    if((lastFrameIndex = inMemoryVideo.Seek(newPlayerPosition, out seekedImage, lastFrameIndex >= 0 ? lastFrameIndex : 0)) < 0)
                    {
                        State = PlayerState.Buffering;
                    }
                    else
                    {
                        CurrentFrame = seekedImage;
                    }

                    if (newPlayerPosition > duration)
                    {
                        State = PlayerState.Stopped;
                    }

                }, TimeSpan.FromMilliseconds(1)));
            }
            else if(State == PlayerState.Stopped)
            {
                pictureFrame.BorderColor = ConsoleColor.Yellow;
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
            else if(State == PlayerState.Failed)
            {
                pictureFrame.BorderColor = ConsoleColor.Red;
                Dialog.ShowMessage(failedMessage.ToRed());
            }
            else
            {
                throw new Exception("Unknown state: "+State);
            }
        }
        
        /// <summary>
        /// Loads a video from a given stream
        /// </summary>
        /// <param name="videoStream">the video stream</param>
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
                                playerProgressBar.ShowPlayCursor = true;
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

                            playerProgressBar.LoadProgressPosition = inMemoryVideo.LoadProgress;
                        });
                        if(AfterFrameLoadDelay.HasValue)
                        {
                            Thread.Sleep(AfterFrameLoadDelay.Value);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.QueueAction(() => 
                    {
                        failedMessage = ex.Message;
                        State = PlayerState.Failed;
                    });
                }
            });
        }
    }
}
