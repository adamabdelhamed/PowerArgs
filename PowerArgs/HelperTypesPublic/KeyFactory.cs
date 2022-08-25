namespace PowerArgs;

/// <summary>
/// A utility for programmatically generating ConsoleKeyInfo events, which can be annoying
/// if trying to map keys to characters. This is mostly used for testing.
/// </summary>
public static class KeyFactory
{
    /// <summary>
    /// Given a ConsoleKey and some optional metadata, produces a ConsoleKeyInfo that is very
    /// similar to what you would get if the user had actually performed a keystroke
    /// </summary>
    /// <param name="key">The ConsoleKey pressed</param>
    /// <param name="shift">true to simulate the shift key being pressed</param>
    /// <param name="alt">true to simulate the alt key being pressed</param>
    /// <param name="control">true to simulate the shift control being pressed</param>
    /// <returns>a ConsoleKeyInfo that maps to the ConsoleKey and options provided</returns>
    public static ConsoleKeyInfo KeyInfo(this ConsoleKey key, bool shift = false, bool alt = false, bool control = false) => 
        new ConsoleKeyInfo(MapChar(key, shift), key, shift, alt, control);


/*
    // This is some helper code that generated the if statements below. Including it in case I ever have to rerun it.
 
    var ret = "";
    var unwriteable = "'\\u0000'";
    var shiftMap = new Dictionary<ConsoleKey, (char normal, char shifted)>()
    {
        { ConsoleKey.D1, ('1','!') },
        { ConsoleKey.D2, ('2','@') },
        { ConsoleKey.D3, ('3','#') },
        { ConsoleKey.D4, ('4','$') },
        { ConsoleKey.D5, ('5','%') },
        { ConsoleKey.D6, ('6','^') },
        { ConsoleKey.D7, ('7','&') },
        { ConsoleKey.D8, ('8','*') },
        { ConsoleKey.D9, ('9','(') },
        { ConsoleKey.D0, ('0',')') },
        { ConsoleKey.OemMinus, ('-','_') },
        { ConsoleKey.OemPlus, ('=','+') },
    };

    foreach (ConsoleKey key in Enum.GetValues(typeof(ConsoleKey)))
    {
        if (key == ConsoleKey.Backspace)
        {
            ret += $"if (key == ConsoleKey.{key}) return '\\b';\n";
        }
        else if (key == ConsoleKey.Tab)
        {
            ret += $"if (key == ConsoleKey.{key}) return '\\t';\n";
        }
        else if (key == ConsoleKey.Spacebar)
        {
            ret += $"if (key == ConsoleKey.{key}) return ' ';\n";
        }
        else if (key == ConsoleKey.Add)
        {
            ret += $"if (key == ConsoleKey.{key}) return '+';\n";
        }
        else if (key == ConsoleKey.Subtract)
        {
            ret += $"if (key == ConsoleKey.{key}) return '-';\n";
        }
        else if (key == ConsoleKey.Multiply)
        {
            ret += $"if (key == ConsoleKey.{key}) return '×';\n";
        }
        else if (key == ConsoleKey.Divide)
        {
            ret += $"if (key == ConsoleKey.{key}) return '/';\n";
        }
        else if (key == ConsoleKey.OemComma)
        {
            ret += $"if (key == ConsoleKey.{key}) return ',';\n";
        }
        else
        {
            var asMapped = shiftMap.ContainsKey(key) ? $"shift ? '{shiftMap[key].shifted}' : '{shiftMap[key].normal}'" : null;

            var isDTypeDigit = key.ToString().Length == 2 && key.ToString()[0] == 'D' && char.IsDigit(key.ToString()[1]);
            var isNumPadDigit = key.ToString().StartsWith("NumPad");
            var asDigit = isDTypeDigit || isNumPadDigit ? $"'{key.ToString().Last()}'" : null;

            var isLetter = key.ToString().Length == 1 && char.IsLetter(key.ToString()[0]);
            var asLetter = isLetter ? $"shift ? '{char.ToUpper(key.ToString()[0])}' : '{char.ToLower(key.ToString()[0])}'" : null;

            var charVal = asMapped ?? asDigit ?? asLetter ?? unwriteable;
            ret += $"if (key == ConsoleKey.{key}) return {charVal};\n";
        }
    }

    Console.WriteLine(ret);
*/

    private static char MapChar(ConsoleKey key, bool shift)
    {
        if (key == ConsoleKey.Backspace) return '\b';
        if (key == ConsoleKey.Tab) return '\t';
        if (key == ConsoleKey.Clear) return '\u0000';
        if (key == ConsoleKey.Enter) return '\u0000';
        if (key == ConsoleKey.Pause) return '\u0000';
        if (key == ConsoleKey.Escape) return '\u0000';
        if (key == ConsoleKey.Spacebar) return ' ';
        if (key == ConsoleKey.PageUp) return '\u0000';
        if (key == ConsoleKey.PageDown) return '\u0000';
        if (key == ConsoleKey.End) return '\u0000';
        if (key == ConsoleKey.Home) return '\u0000';
        if (key == ConsoleKey.LeftArrow) return '\u0000';
        if (key == ConsoleKey.UpArrow) return '\u0000';
        if (key == ConsoleKey.RightArrow) return '\u0000';
        if (key == ConsoleKey.DownArrow) return '\u0000';
        if (key == ConsoleKey.Select) return '\u0000';
        if (key == ConsoleKey.Print) return '\u0000';
        if (key == ConsoleKey.Execute) return '\u0000';
        if (key == ConsoleKey.PrintScreen) return '\u0000';
        if (key == ConsoleKey.Insert) return '\u0000';
        if (key == ConsoleKey.Delete) return '\u0000';
        if (key == ConsoleKey.Help) return '\u0000';
        if (key == ConsoleKey.D0) return shift ? ')' : '0';
        if (key == ConsoleKey.D1) return shift ? '!' : '1';
        if (key == ConsoleKey.D2) return shift ? '@' : '2';
        if (key == ConsoleKey.D3) return shift ? '#' : '3';
        if (key == ConsoleKey.D4) return shift ? '$' : '4';
        if (key == ConsoleKey.D5) return shift ? '%' : '5';
        if (key == ConsoleKey.D6) return shift ? '^' : '6';
        if (key == ConsoleKey.D7) return shift ? '&' : '7';
        if (key == ConsoleKey.D8) return shift ? '*' : '8';
        if (key == ConsoleKey.D9) return shift ? '(' : '9';
        if (key == ConsoleKey.A) return shift ? 'A' : 'a';
        if (key == ConsoleKey.B) return shift ? 'B' : 'b';
        if (key == ConsoleKey.C) return shift ? 'C' : 'c';
        if (key == ConsoleKey.D) return shift ? 'D' : 'd';
        if (key == ConsoleKey.E) return shift ? 'E' : 'e';
        if (key == ConsoleKey.F) return shift ? 'F' : 'f';
        if (key == ConsoleKey.G) return shift ? 'G' : 'g';
        if (key == ConsoleKey.H) return shift ? 'H' : 'h';
        if (key == ConsoleKey.I) return shift ? 'I' : 'i';
        if (key == ConsoleKey.J) return shift ? 'J' : 'j';
        if (key == ConsoleKey.K) return shift ? 'K' : 'k';
        if (key == ConsoleKey.L) return shift ? 'L' : 'l';
        if (key == ConsoleKey.M) return shift ? 'M' : 'm';
        if (key == ConsoleKey.N) return shift ? 'N' : 'n';
        if (key == ConsoleKey.O) return shift ? 'O' : 'o';
        if (key == ConsoleKey.P) return shift ? 'P' : 'p';
        if (key == ConsoleKey.Q) return shift ? 'Q' : 'q';
        if (key == ConsoleKey.R) return shift ? 'R' : 'r';
        if (key == ConsoleKey.S) return shift ? 'S' : 's';
        if (key == ConsoleKey.T) return shift ? 'T' : 't';
        if (key == ConsoleKey.U) return shift ? 'U' : 'u';
        if (key == ConsoleKey.V) return shift ? 'V' : 'v';
        if (key == ConsoleKey.W) return shift ? 'W' : 'w';
        if (key == ConsoleKey.X) return shift ? 'X' : 'x';
        if (key == ConsoleKey.Y) return shift ? 'Y' : 'y';
        if (key == ConsoleKey.Z) return shift ? 'Z' : 'z';
        if (key == ConsoleKey.LeftWindows) return '\u0000';
        if (key == ConsoleKey.RightWindows) return '\u0000';
        if (key == ConsoleKey.Applications) return '\u0000';
        if (key == ConsoleKey.Sleep) return '\u0000';
        if (key == ConsoleKey.NumPad0) return '0';
        if (key == ConsoleKey.NumPad1) return '1';
        if (key == ConsoleKey.NumPad2) return '2';
        if (key == ConsoleKey.NumPad3) return '3';
        if (key == ConsoleKey.NumPad4) return '4';
        if (key == ConsoleKey.NumPad5) return '5';
        if (key == ConsoleKey.NumPad6) return '6';
        if (key == ConsoleKey.NumPad7) return '7';
        if (key == ConsoleKey.NumPad8) return '8';
        if (key == ConsoleKey.NumPad9) return '9';
        if (key == ConsoleKey.Multiply) return '×';
        if (key == ConsoleKey.Add) return '+';
        if (key == ConsoleKey.Separator) return '\u0000';
        if (key == ConsoleKey.Subtract) return '-';
        if (key == ConsoleKey.Decimal) return '\u0000';
        if (key == ConsoleKey.Divide) return '/';
        if (key == ConsoleKey.F1) return '\u0000';
        if (key == ConsoleKey.F2) return '\u0000';
        if (key == ConsoleKey.F3) return '\u0000';
        if (key == ConsoleKey.F4) return '\u0000';
        if (key == ConsoleKey.F5) return '\u0000';
        if (key == ConsoleKey.F6) return '\u0000';
        if (key == ConsoleKey.F7) return '\u0000';
        if (key == ConsoleKey.F8) return '\u0000';
        if (key == ConsoleKey.F9) return '\u0000';
        if (key == ConsoleKey.F10) return '\u0000';
        if (key == ConsoleKey.F11) return '\u0000';
        if (key == ConsoleKey.F12) return '\u0000';
        if (key == ConsoleKey.F13) return '\u0000';
        if (key == ConsoleKey.F14) return '\u0000';
        if (key == ConsoleKey.F15) return '\u0000';
        if (key == ConsoleKey.F16) return '\u0000';
        if (key == ConsoleKey.F17) return '\u0000';
        if (key == ConsoleKey.F18) return '\u0000';
        if (key == ConsoleKey.F19) return '\u0000';
        if (key == ConsoleKey.F20) return '\u0000';
        if (key == ConsoleKey.F21) return '\u0000';
        if (key == ConsoleKey.F22) return '\u0000';
        if (key == ConsoleKey.F23) return '\u0000';
        if (key == ConsoleKey.F24) return '\u0000';
        if (key == ConsoleKey.BrowserBack) return '\u0000';
        if (key == ConsoleKey.BrowserForward) return '\u0000';
        if (key == ConsoleKey.BrowserRefresh) return '\u0000';
        if (key == ConsoleKey.BrowserStop) return '\u0000';
        if (key == ConsoleKey.BrowserSearch) return '\u0000';
        if (key == ConsoleKey.BrowserFavorites) return '\u0000';
        if (key == ConsoleKey.BrowserHome) return '\u0000';
        if (key == ConsoleKey.VolumeMute) return '\u0000';
        if (key == ConsoleKey.VolumeDown) return '\u0000';
        if (key == ConsoleKey.VolumeUp) return '\u0000';
        if (key == ConsoleKey.MediaNext) return '\u0000';
        if (key == ConsoleKey.MediaPrevious) return '\u0000';
        if (key == ConsoleKey.MediaStop) return '\u0000';
        if (key == ConsoleKey.MediaPlay) return '\u0000';
        if (key == ConsoleKey.LaunchMail) return '\u0000';
        if (key == ConsoleKey.LaunchMediaSelect) return '\u0000';
        if (key == ConsoleKey.LaunchApp1) return '\u0000';
        if (key == ConsoleKey.LaunchApp2) return '\u0000';
        if (key == ConsoleKey.Oem1) return '\u0000';
        if (key == ConsoleKey.OemPlus) return shift ? '+' : '=';
        if (key == ConsoleKey.OemComma) return ',';
        if (key == ConsoleKey.OemMinus) return shift ? '_' : '-';
        if (key == ConsoleKey.OemPeriod) return '\u0000';
        if (key == ConsoleKey.Oem2) return '\u0000';
        if (key == ConsoleKey.Oem3) return '\u0000';
        if (key == ConsoleKey.Oem4) return '\u0000';
        if (key == ConsoleKey.Oem5) return '\u0000';
        if (key == ConsoleKey.Oem6) return '\u0000';
        if (key == ConsoleKey.Oem7) return '\u0000';
        if (key == ConsoleKey.Oem8) return '\u0000';
        if (key == ConsoleKey.Oem102) return '\u0000';
        if (key == ConsoleKey.Process) return '\u0000';
        if (key == ConsoleKey.Packet) return '\u0000';
        if (key == ConsoleKey.Attention) return '\u0000';
        if (key == ConsoleKey.CrSel) return '\u0000';
        if (key == ConsoleKey.ExSel) return '\u0000';
        if (key == ConsoleKey.EraseEndOfFile) return '\u0000';
        if (key == ConsoleKey.Play) return '\u0000';
        if (key == ConsoleKey.Zoom) return '\u0000';
        if (key == ConsoleKey.NoName) return '\u0000';
        if (key == ConsoleKey.Pa1) return '\u0000';
        if (key == ConsoleKey.OemClear) return '\u0000';

        return '\u0000';
    }
}

