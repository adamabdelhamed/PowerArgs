namespace PowerArgs.Cli;
public class WorstCasePerfTestPanel : ConsoleControl
{
    private bool _even;
    private ConsoleCharacter evenPen = new ConsoleCharacter('O', ConsoleColor.Black, ConsoleColor.White);
    private ConsoleCharacter oddPen = new ConsoleCharacter('O', ConsoleColor.White, ConsoleColor.Black);
    protected override void OnPaint(ConsoleBitmap context)
    {
        for (var x = 0; x < context.Width; x++)
        {
            for (var y = 0; y < context.Height; y++)
            {
                context.DrawPoint(_even ? evenPen : oddPen, x, y);
                _even = !_even;
            }
        }
        _even = !_even;
        ConsoleApp.Current.RequestPaint();
    }
}
