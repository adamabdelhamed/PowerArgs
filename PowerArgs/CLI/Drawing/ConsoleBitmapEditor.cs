using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A console control that can be used to interactively edit a 
    /// ConsoleBitmap
    /// </summary>
    public class ConsoleBitmapEditor : ConsolePanel
    {
        /// <summary>
        /// An event that fires when the user moves the cursor
        /// </summary>
        public Event CursorMoved { get; private set; } = new Event();

        /// <summary>
        /// The bitmap being edited by this control
        /// </summary>
        public ConsoleBitmap Bitmap { get; private set; }

        /// <summary>
        /// Gets the current cursor position in terms of which pixel it
        /// is covering on the target bitmap
        /// </summary>
        public Point CursorPosition => new Point(cursor.X - 1, cursor.Y - 1);

        /// <summary>
        /// An event that fires when a change has been made to the bitmap by way
        /// of a user edit
        /// </summary>
        public Event BitmapChanged { get; private set; } = new Event();

        private PixelControl cursor;
        private ConsolePanel frame;
        private ConsoleBitmapViewer viewer;
        private ConsoleColor currentFg { get => Get<ConsoleColor>(); set => Set(value); }
        private ConsoleColor currentBg { get => Get<ConsoleColor>(); set => Set(value); }

        /// <summary>
        /// Creates an editor with a new bitmap of the given size
        /// </summary>
        /// <param name="w">the width of the empty bitmap to create</param>
        /// <param name="h">the height of the empty bitmap to create</param>
        public ConsoleBitmapEditor(int w, int h) : this(new ConsoleBitmap(w, h)) { }

        /// <summary>
        /// Creates an editor for the given bitmap
        /// </summary>
        /// <param name="bitmap">the bitmap to edit</param>
        public ConsoleBitmapEditor(ConsoleBitmap bitmap)
        {
            this.Bitmap = bitmap;
            this.Width = bitmap.Width + 2;
            this.Height = bitmap.Height + 3;
            currentFg = ConsoleString.DefaultForegroundColor;
            currentBg = ConsoleString.DefaultBackgroundColor;

            frame = Add(new ConsolePanel() { Background = ConsoleColor.White }).Fill(padding: new Thickness(0, 0, 1, 0));
            viewer = frame.Add(new ConsoleBitmapViewer() { Bitmap = bitmap }).Fill(padding: new Thickness(1, 1, 1, 1));
            cursor = frame.Add(new PixelControl() { IsVisible = false, X = 1, Y = 1, Value = new ConsoleCharacter('C', ConsoleColor.White, ConsoleColor.Cyan) }); // place at top left
            frame.CanFocus = true;

            frame.Focused.SubscribeForLifetime(() => cursor.IsVisible = true, cursor);
            frame.Unfocused.SubscribeForLifetime(() => cursor.IsVisible = false, cursor);
            cursor.CanFocus = false;

            frame.KeyInputReceived.SubscribeForLifetime((key) => HandleCursorKeyPress(key), cursor);

            
            frame.AddedToVisualTree.SubscribeOnce(()=>
            {
                Application.SetTimeout(() =>
                {
                    if (this.IsExpired == false)
                    {
                        frame.TryFocus();
                        CursorMoved.Fire();
                    }
                }, TimeSpan.FromMilliseconds(10));
            });
        }

        /// <summary>
        /// Creates a set of standard buttons that a wrapped control can include.
        /// </summary>
        /// <returns>a set of buttons</returns>
        public IEnumerable<Button> CreateStandardButtons()
        {
            var changeFgButton = new Button() { Shortcut = new KeyboardShortcut(ConsoleKey.F, ConsoleModifiers.Alt) };

            changeFgButton.Pressed.SubscribeForLifetime(() =>
            {
                Dialog.ShowEnumOptions<ConsoleColor>("Choose a color".ToConsoleString()).Then((newColor) =>
                {
                    currentFg = newColor.HasValue ? newColor.Value : currentFg;
                });
            }, this);

            var changeBgButton = new Button() { Shortcut = new KeyboardShortcut(ConsoleKey.B, ConsoleModifiers.Alt) };

            changeBgButton.Pressed.SubscribeForLifetime(() =>
            {
                Dialog.ShowEnumOptions<ConsoleColor>("Choose a color".ToConsoleString()).Then((newColor) =>
                {
                    currentBg = newColor.HasValue ? newColor.Value : currentBg;
                });
            }, this);

            this.SynchronizeForLifetime(nameof(currentFg), () =>
            {
                var displayColor = currentFg == this.Background ? this.Foreground : currentFg;
                changeFgButton.Text = "FG: ".ToConsoleString() + currentFg.ToString().ToConsoleString(displayColor);
            }, this);
            this.SynchronizeForLifetime(nameof(currentBg), () =>
            {
                var displayColor = currentBg == this.Background ? this.Foreground : currentBg;
                changeBgButton.Text = "BG: ".ToConsoleString() + currentBg.ToString().ToConsoleString(displayColor);
            }, this);

            yield return changeFgButton;
            yield return changeBgButton;

        }
        
        private void HandleCursorKeyPress(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.LeftArrow && cursor.X > 1)
            {
                cursor.X--;
                CursorMoved.Fire();
            }
            else if (key.Key == ConsoleKey.RightArrow && cursor.X < Bitmap.Width)
            {
                cursor.X++;
                CursorMoved.Fire();
            }
            else if (key.Key == ConsoleKey.UpArrow && cursor.Y > 1)
            {
                cursor.Y--;
                CursorMoved.Fire();
            }
            else if ((key.Key == ConsoleKey.DownArrow || key.Key == ConsoleKey.Enter) && cursor.Y < Bitmap.Height)
            {
                cursor.Y++;
                CursorMoved.Fire();
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                Bitmap.GetPixel(CursorPosition.X, CursorPosition.Y).Value = null;

                if (cursor.X > 1)
                {
                    cursor.X--;
                }

                CursorMoved.Fire();
            }
            else if (ShouldIgnore(key))
            {
                // ignore
            }
            else
            {
                var targetX = cursor.X - 1;
                var targetY = cursor.Y - 1;
                Bitmap.Pen = new ConsoleCharacter(key.KeyChar, currentFg, currentBg);
                Bitmap.DrawPoint(targetX, targetY);
                BitmapChanged.Fire();
                cursor.X = cursor.X < Bitmap.Width ? cursor.X + 1 : cursor.X;
                CursorMoved.Fire();
            }
        }

        private bool ShouldIgnore(ConsoleKeyInfo key)
        {
            if (key.KeyChar == '\u0000') return true;
            if (key.Key == ConsoleKey.Enter) return true;
            return false;
        }
    }
}
