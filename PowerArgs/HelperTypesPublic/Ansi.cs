using System;
using System.Runtime.CompilerServices;

namespace PowerArgs;
public static class Ansi
{

    public static class Text
    {
        public const string BlinkOff = $"{Esc}[25m";
        public const string BlinkOn = $"{Esc}[5m";
        public const string BoldOff = $"{Esc}[22m";
        public const string BoldOn = $"{Esc}[1m";
        public const string HiddenOn = $"{Esc}[8m";
        public const string ReverseOn = $"{Esc}[7m";
        public const string ReversOff = $"{Esc}[27m";
        public const string StandoutOff = $"{Esc}[23m";
        public const string StandoutOn = $"{Esc}[3m";
        public const string UnderlinedOff = $"{Esc}[24m";
        public const string UnderlinedOn = $"{Esc}[4m";
    }


    public static class Color
    {
        public const string Off = $"{Esc}[0m";

        private static readonly string[] ByteStrings = AllocateByteStrings();

        private static string[] AllocateByteStrings()
        {
            var ret = new string[256];
            for(var i = 0; i < ret.Length; i++)
            {
                ret[i] = i.ToString();
            }
            return ret;
        }

        public class Background
        {
            public static string Rgb(RGB color) => $"{Esc}[48;2;{color.R};{color.G};{color.B}m";

            public static void Rgb(in RGB color, PaintBuffer buffer)
            {
                buffer.Append(Esc);
                buffer.Append("[48;2;");
                buffer.Append(ByteStrings[color.R]);
                buffer.Append(';');
                buffer.Append(ByteStrings[color.G]);
                buffer.Append(';');
                buffer.Append(ByteStrings[color.B]);
                buffer.Append('m');
            }
        }


        public static class Foreground
        {

            public static string Rgb(in RGB color) => $"{Esc}[38;2;{color.R};{color.G};{color.B}m";

            public static void Rgb(in RGB color, PaintBuffer buffer)
            {
                buffer.Append(Esc);
                buffer.Append("[38;2;");
                buffer.Append(ByteStrings[color.R]);
                buffer.Append(";");
                buffer.Append(ByteStrings[color.G]);
                buffer.Append(";");
                buffer.Append(ByteStrings[color.B]);
                buffer.Append("m");
            }
        }
    }


    public static class Cursor
    {
        public const string Off = $"{Esc}[0m";

        private static readonly string[] PositionStrings = AllocatePositionStrings();

        private static string[] AllocatePositionStrings()
        {
            var ret = new string[1000];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = i.ToString();
            }
            return ret;
        }

        public static class Move
        {
            public static string Up(int lines = 1) => $"{Esc}[{lines}A";
            public static string Down(int lines = 1) => $"{Esc}[{lines}B";
            public static string Right(int columns = 1) => $"{Esc}[{columns}C";
            public static string Left(int columns = 1) => $"{Esc}[{columns}D";
            public static string NextLine(int line = 1) => $"{Esc}[{line}E";
            public const string ToUpperLeftCorner  = $"{Esc}[H";
            public static string ToLocation(int left, int top) => $"{Esc}[{top};{left}H";

            public static void ToLocation(int left, int top, PaintBuffer buffer)
            {
                buffer.Append(Esc);
                buffer.Append('[');
                buffer.Append(PositionStrings[top]);
                buffer.Append(';');
                buffer.Append(PositionStrings[left]);
                buffer.Append('H');
            }
        }


        public class Scroll
        {
            public const string UpOne = $"{Esc}D";

            public const string DownOne = $"{Esc}M";
        }

        public const string Hide  = $"{Esc}[?25l";

        public const string Show  = $"{Esc}[?25h";

        public const string SavePosition = $"{Esc}7";

        public const string RestorePosition = $"{Esc}8";
    }

    public static class Clear
    {
        public const string EntireScreen  = $"{Esc}[2J";
        public const string Line  = $"{Esc}[2K";
        public const string ToBeginningOfLine  = $"{Esc}[1K";
        public const string ToBeginningOfScreen  = $"{Esc}[1J";
        public const string ToEndOfLine  = $"{Esc}[K";
        public const string ToEndOfScreen  = $"{Esc}[J";
    }

    public const string Esc = "\u001b";
}

public class PaintBuffer
{
    public char[] Buffer = new char[120 * 80];
    public int Length;
    /*
  
    */
    public void Append(char c)
    {
        EnsureBigEnough(Length + 1);
        Buffer[Length++] = c;
    }

    public void Append(string chars)
    {
        EnsureBigEnough(Length + chars.Length);

        var span = chars.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            Buffer[Length++] = span[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureBigEnough(int newLen)
    {
        while (newLen > Buffer.Length)
        {
            var newBuffer = new char[Buffer.Length * 2];
            Array.Copy(Buffer, 0, newBuffer, 0, Buffer.Length);
            Buffer = newBuffer;
            newLen = Buffer.Length;
        }
    }

    public void Clear()
    {
        Length = 0;
    }
}