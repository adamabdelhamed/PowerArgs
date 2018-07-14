using PowerArgs;
using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
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
            foreach(var c in Characters)
            {
                var fire = new Fire(TimeSpan.FromMilliseconds(10));
                fire.MoveTo(c.Left, c.Top, 10);
                matter.Add(fire);
                Time.CurrentTime.Delay(() => SpaceTime.CurrentSpaceTime.Add(fire), 10 * c.CharIndex);
            }
        }

        public void DrawHotText()
        {
            foreach(var c in Characters)
            {
                var letter = new Wall() { ForcePen = true, Pen = new ConsoleCharacter(c.Symbol, Usually(ConsoleColor.Red, ConsoleColor.DarkRed, .95f)) };
                letter.Tags.Add("indestructible");
                letter.MoveTo(c.Left, c.Top, 5);
                matter.Add(letter);
                Time.CurrentTime.Delay(() => SpaceTime.CurrentSpaceTime.Add(letter), 10 * c.CharIndex);
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
