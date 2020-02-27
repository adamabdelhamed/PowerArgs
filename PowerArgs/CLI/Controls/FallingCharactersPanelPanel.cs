using System;

namespace PowerArgs.Cli
{
    public class FallingCharactersPanel : ConsolePanel
    {
        private static Random r = new Random();

        private ConsoleColor primaryColor;
        private ConsoleColor accentColor;
        private ConsoleColor bgColor;

        public FallingCharactersPanel(ConsoleColor primaryColor, ConsoleColor accentColor, ConsoleColor bgColor)
        {
            this.primaryColor = primaryColor;
            this.accentColor = accentColor;
            this.bgColor = bgColor;
            this.AddedToVisualTree.SubscribeForLifetime(Added, this); 
        }

        private void Added()
        {
            this.OnDisposed(Application.SetInterval(() =>
            {
                var fore = r.NextDouble() < .4;
                var pixel = Add(new PixelControl()
                {
                    CanFocus = false,
                    Value = new ConsoleCharacter((char)r.Next((int)'a', (int)'z'), 
                    foregroundColor: fore ? primaryColor : bgColor, 
                    backgroundColor: fore ? bgColor : (r.NextDouble() < .5 ? primaryColor : accentColor)),
                    X = r.Next(0, Width)
                });

                this.OnDisposed(Application.SetInterval(() =>
                {
                    if (pixel.Y < Height)
                    {
                        pixel.Y++;
                    }
                    else
                    {
                        this.Controls.Remove(pixel);
                    }
                }, TimeSpan.FromMilliseconds(r.Next(3, 25))));

            }, TimeSpan.FromMilliseconds(r.Next(3, 5))));
        }
    }
}
