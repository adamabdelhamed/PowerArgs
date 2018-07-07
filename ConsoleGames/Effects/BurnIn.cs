using PowerArgs;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class BurnIn : TextEffect
    {
        public override void Start()
        {
            ShowFire();
            DrawHotText();

            if (Options.DurationMilliseconds >= 0)
            {
                Time.CurrentTime.Delay(FadeOut, Options.DurationMilliseconds);
            }
        }

        public void ShowFire()
        {
            for(var x = Options.Left; x < Options.Left+Options.Text.Length; x++)
            {
                var fire = new Fire(TimeSpan.FromMilliseconds(500));
                fire.MoveTo(x, Options.Top, 10);
                matter.Add(fire);
                Time.CurrentTime.Delay(() => SpaceTime.CurrentSpaceTime.Add(fire), (int)(10 * (x - Options.Left)));
            }
        }

        public void DrawHotText()
        {
            for (var i = 0; i < Options.Text.Length; i++)
            {
                var letter = new Wall() { ForcePen = true, Pen = new ConsoleCharacter(Options.Text[i], Usually(ConsoleColor.Red, ConsoleColor.DarkRed, .95f)) };
                letter.Tags.Add("indestructible");
                letter.MoveTo(Options.Left + i, Options.Top, 5);
                matter.Add(letter);
                Time.CurrentTime.Delay(() => SpaceTime.CurrentSpaceTime.Add(letter), 10 * i);
            }
        }

        public void FadeOut()
        {
            matter.ForEach(e => Time.CurrentTime.Delay(e.Lifetime.Dispose, (int)(10 * (e.Left - Options.Left))));
            matter.Clear();
            this.Dispose();
        }
    }
}
