using System;

namespace PowerArgs.Cli.Physics
{
    public class StringSpacialElement : SpacialElement
    {
        private ObservableObject observable = new ObservableObject();
        public ConsoleString Content { get => observable.Get<ConsoleString>(); set => observable.Set(value); }

        private bool itsMeResizing;
        private ISizeF prevSize;
        public bool IsVisible { get; set; } = true;
        public StringSpacialElement(ConsoleString content)
        {
            observable.SubscribeForLifetime(nameof(Content), () =>
            {
                itsMeResizing = true;
                this.ResizeTo(Content.Length, this.Height);
                itsMeResizing = false;
            }, this.Lifetime);


            prevSize = SizeF.Create(Bounds.Width, Bounds.Height);
            this.SizeOrPositionChanged.SubscribeForLifetime(() =>
            {
                if (itsMeResizing == false && SizeF.Create(Width,Height).Equals(prevSize) == false) throw new InvalidOperationException($"You can't manually resize elements of type {nameof(StringSpacialElement)}");
                prevSize = SizeF.Create(Bounds.Width, Bounds.Height);
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
