using System;

namespace PowerArgs.Cli
{
    public class MatrixPanel : ConsolePanel
    {
        private static Random r = new Random();
        public MatrixPanel()
        {
            this.AddedToVisualTree.SubscribeForLifetime(Added, this.LifetimeManager); 
        }

        private void Added()
        {
            LifetimeManager.Manage(Application.SetInterval(() =>
            {
                var fore = r.NextDouble() < .4;
                var pixel = Add(new PixelControl()
                {
                    CanFocus = false,
                    Value = new ConsoleCharacter((char)r.Next((int)'a', (int)'z'), 
                    foregroundColor: fore ? ConsoleColor.Green : ConsoleColor.Black, 
                    backgroundColor: fore ? ConsoleColor.Black : (r.NextDouble() < .5 ? ConsoleColor.Green : ConsoleColor.DarkGreen)),
                    X = r.Next(0, Width)
                });

                LifetimeManager.Manage(Application.SetInterval(() =>
                {
                    if (pixel.Y < Height)
                    {
                        pixel.Y++;
                    }
                    else
                    {
                        this.Controls.Remove(pixel);
                    }
                }, TimeSpan.FromMilliseconds(r.Next(20, 100))));

            }, TimeSpan.FromMilliseconds(r.Next(10, 15))));
        }
    }
}
