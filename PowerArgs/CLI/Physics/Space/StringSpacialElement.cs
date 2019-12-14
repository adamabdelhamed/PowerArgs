using System;

namespace PowerArgs.Cli.Physics
{
    public class StringSpacialElement : SpacialElement
    {
        private ObservableObject observable = new ObservableObject();
        public ConsoleString Content { get => observable.Get<ConsoleString>(); set => observable.Set(value); }

        private bool itsMeResizing;
        private IRectangularF prevSize;
        public bool IsVisible { get; set; } = true;
        public StringSpacialElement(ConsoleString content)
        {
            this.Governor.Rate = TimeSpan.FromSeconds(-1);
            observable.SubscribeForLifetime(nameof(Content), () =>
            {
                itsMeResizing = true;
                this.ResizeTo(Content.Length, this.Height);
                itsMeResizing = false;
            }, this.Lifetime);
            

            prevSize = this.CopyBounds();
            this.SizeOrPositionChanged.SubscribeForLifetime(() =>
            {
                if (itsMeResizing == false && SizeF.Create(Width,Height).Equals(prevSize) == false) throw new InvalidOperationException($"You can't manually resize elements of type {nameof(StringSpacialElement)}");
                prevSize = Bounds.CopyBounds();
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
