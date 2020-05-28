namespace PowerArgs.Cli.Physics
{
    public class BorderElement : SpacialElement
    {
        public RGB BorderColor { get; set; }
    }

    [SpacialElementBinding(typeof(BorderElement))]
    public class BorderElementRenderer : SpacialElementRenderer
    {
        public BorderElement BorderElement => Element as BorderElement;

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(' ', backgroundColor: BorderElement.BackgroundColor);
            context.FillRect(0, 0, Width, Height);
            context.Pen = new ConsoleCharacter(' ', backgroundColor: BorderElement.BorderColor);
            context.DrawLine(0, 0, 0, Height);
            context.DrawLine(1, 0, 1, Height);
            context.DrawLine(Width - 1, 0, Width - 1, Height);
            context.DrawLine(Width - 2, 0, Width - 2, Height);
            context.DrawLine(0, 0, Width, 0);
            context.DrawLine(0, Height - 1, Width, Height - 1);
        }
    }
}
