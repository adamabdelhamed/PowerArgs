using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    public struct ConsoleCharacter
    {
        public char Value { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }

        public ConsoleCharacter(char value, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
            : this()
        {
            this.Value = value;

            if (foregroundColor.HasValue == false) foregroundColor = ConsoleString.DefaultForegroundColor;
            if (backgroundColor.HasValue == false) backgroundColor = ConsoleString.DefaultBackgroundColor;

            this.ForegroundColor = foregroundColor.Value;
            this.BackgroundColor = backgroundColor.Value;
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

    public class ConsoleString : List<ConsoleCharacter>, IComparable<string>
    {
        internal static ConsoleColor DefaultForegroundColor;
        internal static ConsoleColor DefaultBackgroundColor;
        
        static ConsoleString ()
        {
            try
            {
                DefaultForegroundColor = Console.ForegroundColor;
                DefaultBackgroundColor = Console.BackgroundColor;
            }
            catch (Exception)
            {
                DefaultForegroundColor = ConsoleColor.Gray;
                DefaultBackgroundColor = ConsoleColor.Black;
            }
        }

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

        public ConsoleString() : this("", DefaultForegroundColor, DefaultBackgroundColor) { }

        public ConsoleString(string value = "", ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
            : base()
        {
            Append(value, foregroundColor, backgroundColor);
        }

        public void Append(string value, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
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

        public ConsoleString Replace(string toFind, string toReplace, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            ConsoleString ret = new ConsoleString();
            ret.Append(this);

            int startIndex = 0;

            while (true)
            {
                string toString = ret.ToString();
                int currentIndex = toString.IndexOf(toFind, startIndex);
                if (currentIndex < 0) break;
                for (int i = 0; i < toFind.Length; i++) ret.RemoveAt(currentIndex);
                ret.InsertRange(currentIndex, toReplace.Select(c => new ConsoleCharacter(c, foregroundColor, backgroundColor)));
                startIndex = currentIndex + toReplace.Length;
            }

            return ret;
        }

        public ConsoleString ReplaceRegex(string regex, string toReplace, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            ConsoleString ret = new ConsoleString();
            ret.Append(this);
            MatchCollection matches = Regex.Matches(this.ToString(), regex);
            foreach (Match match in matches)
            {
                ret = ret.Replace(match.Value, toReplace ?? match.Value, foregroundColor, backgroundColor);
            }

            return ret;
        }

        public int IndexOf(string toFind)
        {
            if(toFind == null)return -1;
            if(toFind == "")return 0;

            int j = 0;
            int k = 0;
            for (int i = 0; i < Length; i++)
            {
                j = 0;
                k = 0;

                while (toFind[j] == this[i + k].Value)
                {
                    j++;
                    k++;
                    if (j == toFind.Length) return i;
                    if (i + k == this.Length) return -1;
                }
            }

            return -1;
        }

        public bool Contains(string substr)
        {
            return IndexOf(substr) >= 0;
        }

        public ConsoleString Substring(int start)
        {
            return Substring(start, this.Length - start);
        }

        public ConsoleString Substring(int start, int length)
        {
            ConsoleString ret = new ConsoleString();
            for(int i = start; i < start + length;i++)
            {
                ret.Add(this[i]);
            }

            return ret;
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


        public int CompareTo(string other)
        {
            return ToString().CompareTo(other);
        }


        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static ConsoleString operator +(ConsoleString a, ConsoleString b)
        {
            if(a == null) return b;
            a.Append(b);
            return a;
        }

        public static ConsoleString operator +(ConsoleString a, string b)
        {
            if (a == null) return b != null ? new ConsoleString(b) : null;
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
