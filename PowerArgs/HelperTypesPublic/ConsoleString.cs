using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    /// <summary>
    /// A wrapper for char that encapsulates foreground and background colors.
    /// </summary>
    public struct ConsoleCharacter
    {
        /// <summary>
        /// The value of the character
        /// </summary>
        public char Value { get; set; }

        /// <summary>
        /// The console foreground color to use when printing this character.
        /// </summary>
        public ConsoleColor ForegroundColor { get; set; }

        /// <summary>
        /// The console background color to use when printing this character.
        /// </summary>
        public ConsoleColor BackgroundColor { get; set; }

        /// <summary>
        /// Create a new ConsoleCharacter given a char value and optionally set the foreground or background coor.
        /// </summary>
        /// <param name="value">The character value</param>
        /// <param name="foregroundColor">The foreground color (defaults to the console's foreground color at initialization time).</param>
        /// <param name="backgroundColor">The background color (defaults to the console's background color at initialization time).</param>
        public ConsoleCharacter(char value, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
            : this()
        {
            this.Value = value;

            if (foregroundColor.HasValue == false) foregroundColor = ConsoleString.DefaultForegroundColor;
            if (backgroundColor.HasValue == false) backgroundColor = ConsoleString.DefaultBackgroundColor;

            this.ForegroundColor = foregroundColor.Value;
            this.BackgroundColor = backgroundColor.Value;
        }

        /// <summary>
        /// Write this formatted character to the console
        /// </summary>
        public void Write()
        {
            ConsoleString.WriteHelper(this.ForegroundColor, this.BackgroundColor, Value);
        }

        /// <summary>
        /// Gets the string representation of the character
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value + "";
        }

        /// <summary>
        /// ConsoleCharacters can be compared to other ConsoleCharacter instances or char values.
        /// </summary>
        /// <param name="obj">The ConsoleCharacter or char to compare to.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {

            if (obj is char) return Value.Equals((char)obj);
            if (obj is ConsoleCharacter == false) return false;
            var other = (ConsoleCharacter)obj;

            return this.Value == other.Value &&
                   this.ForegroundColor == other.ForegroundColor &&
                   this.BackgroundColor == other.BackgroundColor;
        }

        /// <summary>
        /// Operator overload for Equals
        /// </summary>
        /// <param name="a">The first operand</param>
        /// <param name="b">The second operand</param>
        /// <returns></returns>
        public static bool operator ==(ConsoleCharacter a, ConsoleCharacter b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Operator overload for !Equals
        /// </summary>
        /// <param name="a">The first operand</param>
        /// <param name="b">The second operand</param>
        /// <returns></returns>
        public static bool operator !=(ConsoleCharacter a, ConsoleCharacter b)
        {
            return a.Equals(b) == false;
        }


        /// <summary>
        /// Operator overload for Equals
        /// </summary>
        /// <param name="a">The first operand</param>
        /// <param name="b">The second operand</param>
        /// <returns></returns>
        public static bool operator ==(ConsoleCharacter a, char b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Operator overload for !Equals
        /// </summary>
        /// <param name="a">The first operand</param>
        /// <param name="b">The second operand</param>
        /// <returns></returns>
        public static bool operator !=(ConsoleCharacter a, char b)
        {
            return a.Equals(b) == false;
        }

        /// <summary>
        /// Override of GetHashcode that returns the internal char's hashcode.
        /// </summary>
        /// <returns>the internal char's hashcode.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    /// <summary>
    /// A wrapper for string that encapsulates foreground and background colors.  ConsoleStrings are immutable.
    /// </summary>
    public class ConsoleString : List<ConsoleCharacter>, IComparable<string>
    {
        /// <summary>
        /// The console provider to use when writing output
        /// </summary>
        public static IConsoleProvider ConsoleProvider { get; set; }

        internal static ConsoleColor DefaultForegroundColor;
        internal static ConsoleColor DefaultBackgroundColor;
        
        static ConsoleString ()
        {
            ConsoleProvider = new StdConsoleProvider();
            try
            {
                DefaultForegroundColor = ConsoleProvider.ForegroundColor;
                DefaultBackgroundColor = ConsoleProvider.BackgroundColor;
            }
            catch (Exception)
            {
                DefaultForegroundColor = ConsoleColor.Gray;
                DefaultBackgroundColor = ConsoleColor.Black;
            }
        }

        /// <summary>
        /// Represents an empty string.
        /// </summary>
        public static readonly ConsoleString Empty = new ConsoleString();

        /// <summary>
        /// The length of the string.
        /// </summary>
        public int Length
        {
            get
            {
                return Count;
            }
        }

        private bool ContentSet { get; set; }

        /// <summary>
        /// Create a new empty ConsoleString
        /// </summary>
        public ConsoleString() : base()
        {
            ContentSet = false;
            Append(string.Empty);
        }

        /// <summary>
        /// Creates a new ConsoleString from another one
        /// </summary>
        /// <param name="other">The value to copy</param>
        public ConsoleString(ConsoleString other) : base()
        {
            ContentSet = false;
            Append(other);
        }

        /// <summary>
        /// Create a ConsoleString given an initial text value and optional color info.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="foregroundColor">The foreground color (defaults to the console's foreground color at initialization time).</param>
        /// <param name="backgroundColor">The background color (defaults to the console's background color at initialization time).</param>
        public ConsoleString(string value = "", ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
            : base()
        {
            ContentSet = false;
            Append(value, foregroundColor, backgroundColor);
        }

        /// <summary>
        /// Converts a collection of plain strings into ConsoleStrings
        /// </summary>
        /// <param name="plainStrings">the input strings</param>
        /// <param name="foregroundColor">the foreground color of all returned ConsoleStrings</param>
        /// <param name="backgroundColor">the background color of all returned ConsoleStrings</param>
        /// <returns>a collection of ConsoleStrnigs</returns>
        public static List<ConsoleString> ToConsoleStrings(IEnumerable<string> plainStrings, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            return plainStrings.Select(s => new ConsoleString(s, foregroundColor, backgroundColor)).ToList();
        }

        /// <summary>
        /// Appends the given value using the formatting of the last character or the default formatting if this ConsoleString is empty.
        /// </summary>
        /// <param name="value">The string to append.</param>
        public ConsoleString AppendUsingCurrentFormat(string value)
        {
            if (Count == 0)
            {
                return ImmutableAppend(value);
            }
            else
            {
                var prototype = this.Last();
                return ImmutableAppend(value, prototype.ForegroundColor, prototype.BackgroundColor);
            }
        }

        /// <summary>
        /// Replaces all occurrances of the given string with the replacement value using the specified formatting.
        /// </summary>
        /// <param name="toFind">The substring to find</param>
        /// <param name="toReplace">The replacement value</param>
        /// <param name="foregroundColor">The foreground color (defaults to the console's foreground color at initialization time).</param>
        /// <param name="backgroundColor">The background color (defaults to the console's background color at initialization time).</param>
        /// <returns>A new ConsoleString with the replacements.</returns>
        public ConsoleString Replace(string toFind, string toReplace, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            ConsoleString ret = new ConsoleString(this);

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

        /// <summary>
        /// Replaces all matches of the given regular expression with the replacement value using the specified formatting.
        /// </summary>
        /// <param name="regex">The regular expression to find.</param>
        /// <param name="toReplace">The replacement value</param>
        /// <param name="foregroundColor">The foreground color (defaults to the console's foreground color at initialization time).</param>
        /// <param name="backgroundColor">The background color (defaults to the console's background color at initialization time).</param>
        /// <returns></returns>
        public ConsoleString ReplaceRegex(string regex, string toReplace, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            ConsoleString ret = new ConsoleString(this);
            MatchCollection matches = Regex.Matches(this.ToString(), regex);
            foreach (Match match in matches)
            {
                ret = ret.Replace(match.Value, toReplace ?? match.Value, foregroundColor, backgroundColor);
            }

            return ret;
        }

        /// <summary>
        /// Finds the index of a given substring in this ConsoleString.
        /// </summary>
        /// <param name="toFind">The substring to search for.</param>
        /// <returns>The first index of the given substring or -1 if the substring was not found.</returns>
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

        /// <summary>
        /// Determines if this ConsoleString contains the given substring.
        /// </summary>
        /// <param name="substr">The substring to search for.</param>
        /// <returns>True if found, false otherwise.</returns>
        public bool Contains(string substr)
        {
            return IndexOf(substr) >= 0;
        }

        /// <summary>
        /// Get a substring of this ConsoleString starting at the given index.
        /// </summary>
        /// <param name="start">the start index.</param>
        /// <returns>A new ConsoleString representing the substring requested.</returns>
        public ConsoleString Substring(int start)
        {
            return Substring(start, this.Length - start);
        }

        /// <summary>
        /// Get a substring of this ConsoleString starting at the given index and with the given length.
        /// </summary>
        /// <param name="start">the start index.</param>
        /// <param name="length">the number of characters to return</param>
        /// <returns>A new ConsoleString representing the substring requested.</returns>
        public ConsoleString Substring(int start, int length)
        {
            ConsoleString ret = new ConsoleString();
            for(int i = start; i < start + length;i++)
            {
                ret.Add(this[i]);
            }

            return ret;
        }

        /// <summary>
        /// Write this ConsoleString to the console using the desired style.
        /// </summary>
        public void Write()
        {
            string buffer = "";

            ConsoleColor existingForeground = ConsoleProvider.ForegroundColor, existingBackground = ConsoleProvider.BackgroundColor;
            try
            {
                ConsoleColor currentForeground = existingForeground, currentBackground = existingBackground;
                foreach (var character in this)
                {
                    if (character.ForegroundColor != currentForeground ||
                        character.BackgroundColor != currentBackground)
                    {
                        ConsoleProvider.Write(buffer);
                        ConsoleProvider.ForegroundColor = character.ForegroundColor;
                        ConsoleProvider.BackgroundColor = character.BackgroundColor;
                        currentForeground = character.ForegroundColor;
                        currentBackground = character.BackgroundColor;
                        buffer = "";
                    }

                    buffer += character.Value;
                }

                if (buffer.Length > 0) ConsoleProvider.Write(buffer);
            }
            finally
            {
                ConsoleProvider.ForegroundColor = existingForeground;
                ConsoleProvider.BackgroundColor = existingBackground;
            }
        }

        /// <summary>
        /// Write this ConsoleString to the console using the desired style.  A newline is appended.
        /// </summary>
        public void WriteLine()
        {
            var withNewLine = this + Environment.NewLine;
            withNewLine.Write();
        }

        /// <summary>
        /// Get the string representation of this ConsoleString.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new string(this.Select(c => c.Value).ToArray());
        }

        /// <summary>
        /// Compare this ConsoleString to another ConsoleString or a plain string.
        /// </summary>
        /// <param name="obj">The ConsoleString or plain string to compare to.</param>
        /// <returns>True if equal, false otherwise</returns>
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

        /// <summary>
        /// Compare this ConsoleString to another ConsoleString.
        /// </summary>
        /// <param name="other">The ConsoleString to compare to.</param>
        /// <returns>True if equal, false otherwise</returns>
        public int CompareTo(string other)
        {
            return ToString().CompareTo(other);
        }

        /// <summary>
        /// Gets the hashcode of the underlying string
        /// </summary>
        /// <returns>the hashcode of the underlying string</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Operator overload that concatenates 2 ConsoleString instances and returns a new one.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>A new, concatenated ConsoleString</returns>
        public static ConsoleString operator +(ConsoleString a, ConsoleString b)
        {
            if (a == null)
            {
                return b != null ? new ConsoleString(b) :null;
            }
            
            var ret = a.ImmutableAppend(b);
            return ret;
        }

        /// <summary>
        /// Operator overload that concatenates a ConsoleString with a string and returns a new one.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>A new, concatenated ConsoleString</returns>
        public static ConsoleString operator +(ConsoleString a, string b)
        {
            if (a == null)
            {
                return b != null ? new ConsoleString(b) : null;
            }
            var ret = a.ImmutableAppend(b);
            return ret;
        }

        /// <summary>
        /// Compares 2 ConsoleStrings for equality.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>True if they are the same, false otherwise</returns>
        public static bool operator ==(ConsoleString a, ConsoleString b)
        {
            if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
            return a.Equals(b);
        }

        /// <summary>
        /// Compares 2 ConsoleStrings for inequality.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>False if they are the same, true otherwise</returns>
        public static bool operator !=(ConsoleString a, ConsoleString b)
        {
            if (object.ReferenceEquals(a, null)) return !object.ReferenceEquals(b, null);
            return a.Equals(b) == false;
        }

        private ConsoleString ImmutableAppend(string value, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            ConsoleString str = new ConsoleString(this);
            str.ContentSet = false;
            str.Append(value, foregroundColor, backgroundColor);
            return str;
        }

        private ConsoleString ImmutableAppend(ConsoleString other)
        {
            ConsoleString str = new ConsoleString(this);
            str.ContentSet = false;
            str.Append(other);
            return str;
        }


        private void Append(string value, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            if (ContentSet) throw new Exception("ConsoleStrings are immutable");
            foreach (var c in value)
            {
                this.Add(new ConsoleCharacter(c, foregroundColor, backgroundColor));
            }
            ContentSet = true;
        }

        private void Append(ConsoleString other)
        {
            if (ContentSet) throw new Exception("ConsoleStrings are immutable");
            foreach (var c in other)
            {
                this.Add(c);
            }
            ContentSet = true;
        }

        internal static void WriteHelper(ConsoleColor foreground, ConsoleColor background, params char[] text)
        {
            ConsoleColor existingForeground = ConsoleProvider.ForegroundColor, existingBackground = ConsoleProvider.BackgroundColor;

            try
            {
                ConsoleProvider.ForegroundColor = foreground;
                ConsoleProvider.BackgroundColor = background;
                ConsoleProvider.Write(text);
            }
            finally
            {
                ConsoleProvider.ForegroundColor = existingForeground;
                ConsoleProvider.BackgroundColor = existingBackground;
            }
        }
    }
}
