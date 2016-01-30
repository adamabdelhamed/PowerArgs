using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public enum LabelRenderMode
    {
        SingleLineAutoSize,
        MultiLineSmartWrap,
        ManualSizing,
    }

    public class Label : ConsoleControl
    {
        internal static readonly ConsoleString Null = "<null>".ToConsoleString(Theme.DefaultTheme.DisabledColor);

        public ConsoleString Text { get { return Get<ConsoleString>(); } set { Set(value); } }
        public int? MaxWidth{ get { return Get<int?>(); } set { Set(value); } }
        public int? MaxHeight { get { return Get<int?>(); } set { Set(value); } }
        private ConsoleString CleanText
        {
            get
            {
                if (Text == null) return Null;
                return Text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", "    ");
            }
        }

        public LabelRenderMode Mode { get { return Get<LabelRenderMode>(); } set { Set(value); } }

        private List<List<ConsoleCharacter>> lines;

        public Label()
        {
            Height = 1;
            this.Mode = LabelRenderMode.SingleLineAutoSize;
            this.CanFocus = false;
            this.Subscribe(nameof(Text), HandleTextChanged);
            this.Subscribe(nameof(Mode), HandleTextChanged);
            this.Subscribe(nameof(MaxHeight), HandleTextChanged);
            this.Subscribe(nameof(MaxWidth), HandleTextChanged);

            this.Subscribe(nameof(Bounds), HandleTextChanged);
            lines = new List<List<ConsoleCharacter>>();
        }

        private void HandleTextChanged()
        {
            lines.Clear();
            if(Mode == LabelRenderMode.ManualSizing)
            {
                lines.Add(CleanText.ToList());
            }
            else if(Mode == LabelRenderMode.SingleLineAutoSize)
            {
                Height = 1;

                if (MaxWidth.HasValue)
                {
                    Width = Math.Min(MaxWidth.Value, CleanText.Length);
                }
                else
                {
                    Width = CleanText.Length;
                }

                lines.Add(CleanText.ToList());
            }
            else
            {
                DoSmartWrap();
            }
        }

        private void DoSmartWrap()
        {
            List<ConsoleCharacter> currentLine = null;

            var cleaned = CleanText;
            var cleanedString = cleaned.ToString();

            var tokenizer = new Tokenizer<Token>();
            tokenizer.Delimiters.Add(".");
            tokenizer.Delimiters.Add("?");
            tokenizer.Delimiters.Add("!");
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.DelimitAndInclude;
            tokenizer.DoubleQuoteBehavior = DoubleQuoteBehavior.NoSpecialHandling;
            var tokens = tokenizer.Tokenize(cleanedString);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (currentLine == null)
                {
                    SmartWrapNewLine(lines, ref currentLine);
                }

                if (token.Value == "\n")
                {
                    SmartWrapNewLine(lines, ref currentLine);
                }
                else if (currentLine.Count + token.Value.Length <= Width)
                {
                    currentLine.AddRange(cleaned.Substring(token.StartIndex, token.Value.Length));
                }
                else
                {
                    SmartWrapNewLine(lines, ref currentLine);

                    var toAdd = cleaned.Substring(token.StartIndex, token.Value.Length);

                    foreach (var c in toAdd)
                    {
                        if (currentLine.Count == Width)
                        {
                            SmartWrapNewLine(lines, ref currentLine);
                        }
                        currentLine.Add(c);
                    }
                }
            }

            if (MaxHeight.HasValue)
            {
                Height = Math.Min(lines.Count, MaxHeight.Value);
            }
            else
            {
                Height = lines.Count;
            }
        }

        private void SmartWrapNewLine(List<List<ConsoleCharacter>> lines, ref List<ConsoleCharacter> currentLine)
        {
            currentLine = new List<ConsoleCharacter>();
            lines.Add(currentLine);
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            for(int y = 0; y < lines.Count; y++)
            {
                if(y >= Height)
                {
                    break;
                }

                var line = lines[y];

                for(int x = 0; x < line.Count && x < Width; x++)
                {
                    context.Pen = HasFocus ? new ConsoleCharacter(line[x].Value, Application.Theme.FocusContrastColor, Application.Theme.FocusColor) : line[x];
                    context.DrawPoint(x, y);
                }
            }
        }
    }
}
