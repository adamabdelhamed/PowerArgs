using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public struct ConsoleCharacter
    {
        public char Value { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }

        public ConsoleCharacter(char value, ConsoleColor foregroundColor = ConsoleString.DefaultForegroundColor, ConsoleColor backgroundColor = ConsoleString.DefaultBackgroundColor)
            : this()
        {
            this.Value = value;
            this.ForegroundColor = foregroundColor;
            this.BackgroundColor = backgroundColor;
        }

        public void Write()
        {
            ConsoleString.WriteHelper(this.ForegroundColor, this.BackgroundColor, Value);
        }

        public override string ToString()
        {
            return Value + "";
        }

        public override bool Equals(object obj)
        {

            if (obj is char) return Value.Equals((char)obj);
            if (obj is ConsoleCharacter == false) return false;
            var other = (ConsoleCharacter)obj;

            return this.Value == other.Value &&
                   this.ForegroundColor == other.ForegroundColor &&
                   this.BackgroundColor == other.BackgroundColor;
        }

        public static bool operator ==(ConsoleCharacter a, ConsoleCharacter b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ConsoleCharacter a, ConsoleCharacter b)
        {
            return a.Equals(b) == false;
        }


        public static bool operator ==(ConsoleCharacter a, char b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ConsoleCharacter a, char b)
        {
            return a.Equals(b) == false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class ConsoleString : List<ConsoleCharacter>
    {
        internal const ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;
        internal const ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;

        public static ConsoleString Empty
        {
            get
            {
                return new ConsoleString(string.Empty);
            }
        }

        public int Length
        {
            get
            {
                return Count;
            }
        }

        public ConsoleString(string value = "", ConsoleColor foregroundColor = DefaultForegroundColor, ConsoleColor backgroundColor = DefaultBackgroundColor)
            : base()
        {
            Append(value, foregroundColor, backgroundColor);
        }

        public void Append(string value, ConsoleColor foregroundColor = DefaultForegroundColor, ConsoleColor backgroundColor = DefaultBackgroundColor)
        {
            foreach (var c in value)
            {
                this.Add(new ConsoleCharacter(c, foregroundColor, backgroundColor));
            }
        }

        public void Append(ConsoleString other)
        {
            foreach (var c in other.ToArray()) // ToArray() prevents concurrent modification when a and b refer to the same object
            {
                this.Add(c);
            }
        }

        public void AppendUsingCurrentFormat(string value)
        {
            if (Count == 0)
            {
                Append(value);
            }
            else
            {
                var prototype = this.Last();
                Append(value, prototype.ForegroundColor, prototype.BackgroundColor);
            }
        }

        public void Write()
        {
            foreach (var character in this)
            {
                character.Write();
            }
        }

        public override string ToString()
        {
            return new string(this.Select(c => c.Value).ToArray());
        }

        public override bool Equals(object obj)
        {
            if (obj is string) return ToString().Equals(obj as string);

            ConsoleString other = obj as ConsoleString;
            if (object.ReferenceEquals(other, null)) return false;
            if (other.Length != this.Length) return false;


            for (int i = 0; i < this.Length; i++)
            {
                if (this[i] != other[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static ConsoleString operator +(ConsoleString a, ConsoleString b)
        {
            if (object.ReferenceEquals(a, null)) return b;
            a.Append(b);
            return a;
        }

        public static ConsoleString operator +(ConsoleString a, string b)
        {
            if (object.ReferenceEquals(a, null)) return b != null ? new ConsoleString(b) : null;
            a.Append(b);
            return a;
        }

        public static bool operator ==(ConsoleString a, ConsoleString b)
        {
            if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
            return a.Equals(b);
        }

        public static bool operator !=(ConsoleString a, ConsoleString b)
        {
            if (object.ReferenceEquals(a, null)) return !object.ReferenceEquals(b, null);
            return a.Equals(b) == false;
        }

        internal static void WriteHelper(ConsoleColor foreground, ConsoleColor background, params char[] text)
        {
            ConsoleColor existingForeground = Console.ForegroundColor, existingBackground = Console.BackgroundColor;

            try
            {
                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;
                Console.Write(text);
            }
            finally
            {
                Console.ForegroundColor = existingForeground;
                Console.BackgroundColor = existingBackground;
            }
        }
    }
}
