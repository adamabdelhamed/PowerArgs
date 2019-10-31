namespace PowerArgs.Cli.Physics
{
    public class StringSpacialElement : SpacialElement
    {
        private ObservableObject observable = new ObservableObject();
        public ConsoleString Content { get => observable.Get<ConsoleString>(); set => observable.Set(value); }

        public StringSpacialElement(ConsoleString content)
        {
            Content = content;
            ResizeTo(content.Length, 1);
            observable.SubscribeForLifetime(nameof(Content), ()=> this.ResizeTo(Content.Length, this.Height), this.Lifetime);
        }
    }

    [SpacialElementBinding(typeof(StringSpacialElement))]
    public class StringSpacialElementRenderer : SpacialElementRenderer
    {
        public StringSpacialElement StringSpacialElement => Element as StringSpacialElement;
        public StringSpacialElementRenderer()
        {
            TransparentBackground = true;
        }
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(StringSpacialElement.Content, 0, 0);
    }
}
