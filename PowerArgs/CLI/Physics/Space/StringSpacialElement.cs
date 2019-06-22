namespace PowerArgs.Cli.Physics
{
    public class StringSpacialElement : SpacialElement
    {
        public ConsoleString Content { get; set; }

        public StringSpacialElement(ConsoleString content)
        {
            Content = content;
            ResizeTo(content.Length, 1);
        }
    }

    [SpacialElementBinding(typeof(StringSpacialElement))]
    public class StringSpacialElementRenderer : SpacialElementRenderer
    {
        public StringSpacialElement StringSpacialElement => Element as StringSpacialElement;
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(StringSpacialElement.Content, 0, 0);
    }
}
