namespace PowerArgs.Cli
{
    internal class FillMarkupProcessor : IMarkupProcessor
    {
        public void Process(ParserContext context)
        {
            var textValue = context.CurrentElement["Fill"];
            var paddingVal = context.CurrentElement["Fill-Padding"];
            var padding = paddingVal == null ? new Thickness() : Thickness.Parse(paddingVal);

            if (textValue == "Horizontal")
            {
                context.CurrentControl.FillHorizontally(padding: padding);
            }
            else if (textValue == "Vertical")
            {
                context.CurrentControl.FillVertically(padding: padding);
            }
            else if (textValue == "Both")
            {
                context.CurrentControl.Fill(padding: padding);
            }
        }
    }
}
