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

        private ConsoleString _cleanCache;
        /// <summary>
        /// Gets or sets the text displayed on the label
        /// </summary>
        public ConsoleString Text { get { return Get<ConsoleString>(); } set { _cleanCache = null; Set(value); } }

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
                _cleanCache = _cleanCache ?? Text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", "    ");
                return _cleanCache;
            }
        }

        private LabelRenderMode _mode;
        /// <summary>
        /// Gets or sets the render mode
        /// </summary>
        public LabelRenderMode Mode { get { return _mode; } set { SetHardIf(ref _mode, value, value != _mode); } }

        private List<List<ConsoleCharacter>> lines;

        private static Action<object> TextChangedHandler = HandleTextChanged;

        /// <summary>
        /// Creates a new label
        /// </summary>
        public Label()
        {
            Height = 1;
            this.Mode = LabelRenderMode.SingleLineAutoSize;
            this.CanFocus = false;
            lines = new List<List<ConsoleCharacter>>();

            this.SubscribeForLifetime(nameof(Text), TextChangedHandler,this, this);
            this.SubscribeForLifetime(nameof(Mode), TextChangedHandler,this, this);
            this.SubscribeForLifetime(nameof(MaxHeight), TextChangedHandler,this, this);
            this.SubscribeForLifetime(nameof(MaxWidth), TextChangedHandler,this, this);
            this.SynchronizeForLifetime(nameof(Bounds), TextChangedHandler,this, this);
            Text = ConsoleString.Empty;
        }

        public static ConsolePanel CreatePanelWithCenteredLabel(ConsoleString str)
        {
            var ret = new ConsolePanel();
            ret.Add(new Label() { Text = str }).CenterBoth();
            return ret;
        }

        private static void HandleTextChanged(object l)
        {
            var label = l as Label;
            label.lines.Clear();
            var clean = label.CleanText;
            if (label.Mode == LabelRenderMode.ManualSizing)
            {
                label.lines.Add(new List<ConsoleCharacter>());
                foreach (var c in clean)
                {
                    if (c.Value == '\n')
                    {
                        label.lines.Add(new List<ConsoleCharacter>());
                    }
                    else
                    {
                        label.lines.Last().Add(c);
                    }
                }
            }
            else if (label.Mode == LabelRenderMode.SingleLineAutoSize)
            {
                label.Height = 1;

                if (label.MaxWidth.HasValue)
                {
                    label.Width = Math.Min(label.MaxWidth.Value, clean.Length);
                }
                else
                {
                    label.Width = clean.Length;
                }

                label.lines.Add(clean.ToList());
            }
            else
            {
                label.DoSmartWrap();
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
