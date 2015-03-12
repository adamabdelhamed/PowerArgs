using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    /// <summary>
    /// A wrapper for char that encapsulates foreground and background colors.
    /// </summary>
    public struct ConsoleCharacter : ICanBeAConsoleString
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
            new ConsoleString(new ConsoleCharacter[] { this }).Write();
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

        /// <summary>
        /// Formats this object as a ConsoleString
        /// </summary>
        /// <returns>a ConsoleString</returns>
        public ConsoleString ToConsoleString()
        {
            return new ConsoleString(new ConsoleCharacter[] { this });
        }
    }

    /// <summary>
    /// A wrapper for string that encapsulates foreground and background colors.  ConsoleStrings are immutable.
    /// </summary>
    public class ConsoleString : IEnumerable<ConsoleCharacter>, IComparable<string>, ICanBeAConsoleString
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

        private List<ConsoleCharacter> characters;

        /// <summary>
        /// Gets the string value of this ConsoleString.  Useful when using the debugger.
        /// </summary>
        public string StringValue
        {
            get
            {
                return ToString();
            }
        }

        /// <summary>
        /// The length of the string.
        /// </summary>
        public int Length
        {
            get
            {
                return characters.Count;
            }
        }

        private bool ContentSet { get; set; }

        /// <summary>
        /// Create a new empty ConsoleString
        /// </summary>
        public ConsoleString()
        {
            characters = new List<ConsoleCharacter>();
            ContentSet = false;
            Append(string.Empty);
        }

        /// <summary>
        /// Creates a new ConsoleString from a collection of ConsoleCharacter objects
        /// </summary>
        /// <param name="chars">The value to use to seed this string</param>
        public ConsoleString(IEnumerable<ConsoleCharacter> chars)
        {
            characters = new List<ConsoleCharacter>();
            ContentSet = false;
            Append(chars);
        }

        /// <summary>
        /// Create a ConsoleString given an initial text value and optional color info.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="foregroundColor">The foreground color (defaults to the console's foreground color at initialization time).</param>
        /// <param name="backgroundColor">The background color (defaults to the console's background color at initialization time).</param>
        public ConsoleString(string value = "", ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            characters = new List<ConsoleCharacter>();
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
        /// Returns a new string that Appends the given value to this one using the formatting of the last character or the default formatting if this ConsoleString is empty.
        /// </summary>
        /// <param name="value">The string to append.</param>
        public ConsoleString AppendUsingCurrentFormat(string value)
        {
            if (Length == 0)
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
        /// <param name="comparison">Specifies how characters are compared</param>
        /// <returns>A new ConsoleString with the replacements.</returns>
        public ConsoleString Replace(string toFind, string toReplace, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, StringComparison comparison = StringComparison.InvariantCulture)
        {
            ConsoleString ret = new ConsoleString(this);

            int startIndex = 0;

            while (true)
            {
                string toString = ret.ToString();
                int currentIndex = toString.IndexOf(toFind, startIndex, comparison);
                if (currentIndex < 0) break;
                for (int i = 0; i < toFind.Length; i++) ret.characters.RemoveAt(currentIndex);
                ret.characters.InsertRange(currentIndex, toReplace.Select(c => new ConsoleCharacter(c, foregroundColor, backgroundColor)));
                startIndex = currentIndex + toReplace.Length;
            }

            return ret;
        }


        /// <summary>
        /// Highights all occurrances of the given string with the desired foreground and background color.
        /// </summary>
        /// <param name="toFind">The substring to find</param>
        /// <param name="foregroundColor">The foreground color (defaults to the console's foreground color at initialization time).</param>
        /// <param name="backgroundColor">The background color (defaults to the console's background color at initialization time).</param>
        /// <param name="comparison">Specifies how characters are compared</param>
        /// <returns>A new ConsoleString with the highlights.</returns>
        public ConsoleString Highlight(string toFind,ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, StringComparison comparison = StringComparison.InvariantCulture)
        {
            ConsoleString ret = new ConsoleString(this);

            int startIndex = 0;

            while (true)
            {
                string toString = ret.ToString();
                int currentIndex = toString.IndexOf(toFind, startIndex, comparison);
                if (currentIndex < 0) break;

                string replacement = "";
                for (int i = 0; i < toFind.Length; i++)
                {
                    replacement += ret.characters[currentIndex].Value;
                    ret.characters.RemoveAt(currentIndex);
                }
                ret.characters.InsertRange(currentIndex, replacement.Select(c => new ConsoleCharacter(c, foregroundColor, backgroundColor)));
                startIndex = currentIndex + replacement.Length;
            }

            return ret;
        }

        /// <summary>
        /// Creates a new ConsoleString with the sam characters as this one, but with a 
        /// new background color
        /// </summary>
        /// <param name="bg">the new background color</param>
        /// <returns>A  new string with a different background color</returns>
        public ConsoleString ToDifferentBackground(ConsoleColor? bg)
        {
            List<ConsoleCharacter> ret = new List<ConsoleCharacter>();
            foreach(var c in this)
            {
                ret.Add(new ConsoleCharacter(c.Value, c.ForegroundColor, bg));
            }
            return new ConsoleString(ret);
        }

        /// <summary>
        /// Returns a new ConsoleString that is a copy of this ConsoleString, but applies the given style to the range of characters specified.
        /// </summary>
        /// <param name="start">the start index to apply the highlight</param>
        /// <param name="length">the number of characters to apply the highlight</param>
        /// <param name="foregroundColor">the foreground color to apply to the highlighted characters or null to use the default foreground color</param>
        /// <param name="backgroundColor">the background color to apply to the highlighted characters or null to use the default background color</param>
        /// <returns>a new ConsoleString that is a copy of this ConsoleString, but applies the given style to the range of characters specified.</returns>
        public ConsoleString HighlightSubstring(int start, int length, ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null)
        {
            List<ConsoleCharacter> buffer = new List<ConsoleCharacter>();
            
            for(int i = 0; i < this.Length; i++)
            {
                if(i >= start && i < start+length)
                {
                    buffer.Add(new ConsoleCharacter(this[i].Value, foregroundColor, backgroundColor));
                }
                else
                {
                    buffer.Add(this[i]);
                }
            }

            return new ConsoleString(buffer);
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
        /// <param name="comparison">Specifies how characters are compared</param>
        /// <returns>The first index of the given substring or -1 if the substring was not found.</returns>
        public int IndexOf(string toFind, StringComparison comparison = StringComparison.InvariantCulture)
        {
            if(toFind == null)return -1;
            if(toFind == "")return 0;

            int j = 0;
            int k = 0;
            for (int i = 0; i < Length; i++)
            {
                j = 0;
                k = 0;

                while ((toFind[j]+"").Equals(""+characters[i + k].Value, comparison))
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
        /// Determines if this ConsoleString starts with the given string
        /// </summary>
        /// <param name="substr">the substring to look for</param>
        /// <param name="comparison">Specifies how characters are compared</param>
        /// <returns>true if this ConsoleString starts with the given substring, false otherwise</returns>
        public bool StartsWith(string substr, StringComparison comparison = StringComparison.InvariantCulture)
        {
            return IndexOf(substr, comparison) == 0;
        }

        /// <summary>
        /// Determines if this ConsoleString ends with the given string
        /// </summary>
        /// <param name="substr">the substring to look for</param>
        /// <param name="comparison">Specifies how characters are compared</param>
        /// <returns>true if this ConsoleString ends with the given substring, false otherwise</returns>
        public bool EndsWith(string substr, StringComparison comparison = StringComparison.InvariantCulture)
        {
            return substr.Length <= this.Length && Substring(Length - substr.Length).StringValue.Equals(substr, comparison);
        }

        /// <summary>
        /// Determines if this ConsoleString contains the given substring.
        /// </summary>
        /// <param name="substr">The substring to search for.</param>
        /// <param name="comparison">Specifies how characters are compared</param>
        /// <returns>True if found, false otherwise.</returns>
        public bool Contains(string substr, StringComparison comparison = StringComparison.InvariantCulture)
        {
            return IndexOf(substr, comparison) >= 0;
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
                ret.characters.Add(this.characters[i]);
            }

            return ret;
        }
        
        /// <summary>
        /// Writes the string representation of the given object to the console using the specified colors.
        /// </summary>
        /// <param name="o">The object to write</param>
        /// <param name="fg">The foreground color to use</param>
        /// <param name="bg">The background color to use</param>
        public static void Write(object o, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            string str = o == null ? "" : o.ToString();
            new ConsoleString(str, fg, bg).Write();
        }

        /// <summary>
        /// Writes the string representation of the given object to the console using the specified colors and appends a newline.
        /// </summary>
        /// <param name="o">The object to write</param>
        /// <param name="fg">The foreground color to use</param>
        /// <param name="bg">The background color to use</param>
        public static void WriteLine(object o, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            string str = o == null ? "" : o.ToString();
            new ConsoleString(str, fg, bg).WriteLine();
        }
        
        /// <summary>
        /// Writes the given ConsoleString to the console
        /// </summary>
        /// <param name="str">the string to write</param>
        public static void Write(ConsoleString str)
        {
            str.Write();
        }

        /// <summary>
        /// Writes the given ConsoleString to the console and appends a newline
        /// </summary>
        /// <param name="str">the string to write</param>
        public static void WriteLine(ConsoleString str)
        {
            str.WriteLine();
        }

        /// <summary>
        /// Writes the string to the console using the specified colors.
        /// </summary>
        /// <param name="str">The object to write</param>
        /// <param name="fg">The foreground color to use</param>
        /// <param name="bg">The background color to use</param>
        public static void Write(string str, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            new ConsoleString(str, fg, bg).Write();
        }

        /// <summary>
        /// Writes the string to the console using the specified colors and appends a newline.
        /// </summary>
        /// <param name="str">The object to write</param>
        /// <param name="fg">The foreground color to use</param>
        /// <param name="bg">The background color to use</param>
        public static void WriteLine(string str, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            new ConsoleString(str, fg, bg).WriteLine();
        }

        /// <summary>
        /// Write this ConsoleString to the console using the desired style.
        /// </summary>
        public void Write()
        {
            if (ConsoleOutInterceptor.Instance.IsInitialized)
            {
                ConsoleOutInterceptor.Instance.Write(this);
            }
            else
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
                if (this.characters[i] != other.characters[i]) return false;
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

        /// <summary>
        /// Gets the character at the specified index
        /// </summary>
        /// <param name="index">the index of the character to find</param>
        /// <returns>the character at the specified index</returns>
        public ConsoleCharacter this[int index]
        {
            get
            {
                return characters[index];
            }
        }

        /// <summary>
        /// Formats this object as a ConsoleString
        /// </summary>
        /// <returns>a ConsoleString</returns>
        public ConsoleString ToConsoleString()
        {
            return this;
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
                this.characters.Add(new ConsoleCharacter(c, foregroundColor, backgroundColor));
            }
            ContentSet = true;
        }

        private void Append(IEnumerable<ConsoleCharacter> chars)
        {
            if (ContentSet) throw new Exception("ConsoleStrings are immutable");
            foreach (var c in chars)
            {
                this.characters.Add(c);
            }
            ContentSet = true;
        }

        /// <summary>
        /// Gets an enumerator for this string
        /// </summary>
        /// <returns>an enumerator for this string</returns>
        public IEnumerator<ConsoleCharacter> GetEnumerator()
        {
            return characters.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for this string
        /// </summary>
        /// <returns>an enumerator for this string</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return characters.GetEnumerator();
        }
    }

    /// <summary>
    /// Extensions that make it easy to work with ConsoleStrings
    /// </summary>
    public static class ConsoleStringX
    {
        /// <summary>
        /// Converts the given enumeration of console characters to a console string
        /// </summary>
        /// <param name="buffer">the characters to convert to a console string</param>
        /// <returns>the new console string</returns>
        public static ConsoleString ToConsoleString(this IEnumerable<ConsoleCharacter> buffer)
        {
            return new ConsoleString(buffer);
        }

        /// <summary>
        /// Converts the given enumeration of console characters to a normal string
        /// </summary>
        /// <param name="buffer">the characters to convert to a normal string</param>
        /// <returns>the new string</returns>
        public static string ToNormalString(this IEnumerable<ConsoleCharacter> buffer)
        {
            return buffer.ToConsoleString().ToString();
        }
    }
}
