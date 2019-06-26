using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Determines how a label renders
    /// </summary>
    public enum LabelRenderMode
    {
        /// <summary>
        /// Render the text on a single line and auto size the width based on the text
        /// </summary>
        SingleLineAutoSize,
        /// <summary>
        /// Render on multiple lines, breaking spaces and punctuation near the control's width.  Good for paragraph text.
        /// </summary>
        MultiLineSmartWrap,
        /// <summary>
        /// Manually size the label, truncation can occur
        /// </summary>
        ManualSizing,
    }

    /// <summary>
    /// A control that displays text
    /// </summary>
    public class Label : ConsoleControl
    {
        internal static readonly ConsoleString Null = "<null>".ToConsoleString(DefaultColors.DisabledColor);

        /// <summary>
        /// Gets or sets the text displayed on the label
        /// </summary>
        public ConsoleString Text { get { return Get<ConsoleString>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets the max width.  This is only used in the single line auto size mode.
        /// </summary>
        public int? MaxWidth { get { return Get<int?>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets the max height.  This is only used in the multi line smart wrap mode.
        /// </summary>
        public int? MaxHeight { get { return Get<int?>(); } set { Set(value); } }
        private ConsoleString CleanText
        {
            get
            {
                if (Text == null) return Null;
                return Text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", "    ");
            }
        }

        /// <summary>
        /// Gets or sets the render mode
        /// </summary>
        public LabelRenderMode Mode { get { return Get<LabelRenderMode>(); } set { Set(value); } }

        private List<List<ConsoleCharacter>> lines;

        /// <summary>
        /// Creates a new label
        /// </summary>
        public Label()
        {
            Height = 1;
            this.Mode = LabelRenderMode.SingleLineAutoSize;
            this.CanFocus = false;
            lines = new List<List<ConsoleCharacter>>();

            this.SubscribeForLifetime(nameof(Text), HandleTextChanged, this);
            this.SubscribeForLifetime(nameof(Mode), HandleTextChanged, this);
            this.SubscribeForLifetime(nameof(MaxHeight), HandleTextChanged, this);
            this.SubscribeForLifetime(nameof(MaxWidth), HandleTextChanged, this);
            this.SynchronizeForLifetime(nameof(Bounds), HandleTextChanged, this);
        }

        private void HandleTextChanged()
        {
            lines.Clear();
            if (Mode == LabelRenderMode.ManualSizing)
            {
                lines.Add(new List<ConsoleCharacter>());
                foreach (var c in CleanText)
                {
                    if (c.Value == '\n')
                    {
                        lines.Add(new List<ConsoleCharacter>());
                    }
                    else
                    {
                        lines.Last().Add(c);
                    }
                }
            }
            else if (Mode == LabelRenderMode.SingleLineAutoSize)
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

                    var toAdd = cleaned.Substring(token.StartIndex, token.Value.Length).TrimStart();

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

        protected override void OnPaint(ConsoleBitmap context)
        {
            for (int y = 0; y < lines.Count; y++)
            {
                if (y >= Height)
                {
                    break;
                }

                var line = lines[y];

                for (int x = 0; x < line.Count && x < Width; x++)
                {
                    context.Pen = HasFocus ? new ConsoleCharacter(line[x].Value, DefaultColors.FocusContrastColor, DefaultColors.FocusColor) : line[x];
                    context.DrawPoint(x, y);
                }
            }
        }
    }
}
