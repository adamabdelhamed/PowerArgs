using System;

namespace PowerArgs.Cli.Physics
{
    public class StringSpacialElement : SpacialElement
    {
        private ObservableObject observable = new ObservableObject();
        public ConsoleString Content { get => observable.Get<ConsoleString>(); set => observable.Set(value); }

        private bool itsMeResizing;
        private float prevW;
        private float prevH;
        public bool IsVisible { get; set; } = true;
        public StringSpacialElement(ConsoleString content)
        {
            observable.SubscribeForLifetime(nameof(Content), () =>
            {
                itsMeResizing = true;
                this.ResizeTo(Content.Length, this.Height);
                itsMeResizing = false;
            }, this.Lifetime);

            prevW = Width;
            prevH = Height;
            this.SizeOrPositionChanged.SubscribeForLifetime(() =>
            {
                
                if (itsMeResizing == false && (Width != prevW || Height != prevH)) throw new InvalidOperationException($"You can't manually resize elements of type {nameof(StringSpacialElement)}");
                prevW = Width;
                prevH = Height;
            }, this.Lifetime);

            Content = content;
        }
    }

    [SpacialElementBinding(typeof(StringSpacialElement))]
    public class StringSpacialElementRenderer : SpacialElementRenderer
    {
        public StringSpacialElement StringSpacialElement => Element as StringSpacialElement;

        private ConsoleString content;
        public StringSpacialElementRenderer()
        {
            TransparentBackground = true;
        }

        public override void OnRender()
        {
            base.OnRender();
            this.IsVisible = StringSpacialElement.IsVisible;
            this.content = new ConsoleString(StringSpacialElement.Content);
        }

        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(content, 0, 0);
    }
}
