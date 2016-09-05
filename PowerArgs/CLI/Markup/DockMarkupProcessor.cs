namespace PowerArgs.Cli
{
    public class DockMarkupProcessor : IMarkupProcessor
    {
        public void Process(ParserContext context)
        {
            var textValue = context.CurrentElement["Dock"];
            var paddingVal = context.CurrentElement["Dock-Padding"];
            var padding = paddingVal == null ? 0 : int.Parse(paddingVal);

            if (textValue == "Left")
            {
                context.CurrentControl.DockToLeft(padding: padding);
            }
            else if (textValue == "Right")
            {
                context.CurrentControl.DockToRight(padding: padding);
            }
            else if (textValue == "Top")
            {
                context.CurrentControl.DockToTop(padding: padding);
            }
            else if (textValue == "Bottom")
            {
                context.CurrentControl.DockToBottom(padding: padding);
            }
        }
    }
}
