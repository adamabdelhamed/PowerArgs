using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    public abstract class GroupedRegexArg
    {
        protected Match exactMatch;
        protected GroupedRegexArg(string regex, string input, string errorMessage)
        {
            MatchCollection matches = Regex.Matches(input, regex);
            exactMatch = (from m in matches.ToList() where m.Value == input select m).SingleOrDefault();
            if (exactMatch == null) throw new ArgException(errorMessage + ": " + input);
        }

        protected static string Group(string regex, string groupName = null)
        {
            return groupName == null ? "(" + regex + ")" : "(?<" + groupName + ">" + regex + ")";
        }

        protected string this[string groupName]
        {
            get
            {
                return this.exactMatch.Groups[groupName].Value;
            }
        }

        protected string this[int groupNumber]
        {
            get
            {
                return this.exactMatch.Groups[groupNumber].Value;
            }
        }
    }

    public class USPhoneNumber : GroupedRegexArg
    {
        public string AreaCode { get; private set; }
        public string FirstDigits { get; private set; }
        public string SecondDigits { get; private set; }

        /// <summary>
        /// Creates a phone number object from a string
        /// </summary>
        /// <param name="phoneNumber"></param>
        public USPhoneNumber(string phoneNumber) : base(PhoneNumberRegex,phoneNumber, "Invalid phone number")
        {
            this.AreaCode = base["areaCode"];
            this.FirstDigits = base["firstDigits"];
            this.SecondDigits = base["secondDigits"];
        }

        public override string ToString()
        {
            return this.ToString("1-({aaa})-{bbb}-{cccc}");
        }


        /// <summary>
        /// Formats the phone number as a string.  
        /// </summary>
        /// <param name="format">Use '{aaa}' for the area code, use {bbb} for the first grouping, and use {cccc} for the second grouping.</param>
        /// <returns>A formatted phone number string</returns>
        public string ToString(string format)
        {
            return format.Replace("{aaa}", this.AreaCode)
                        .Replace("{bbb}", this.FirstDigits)
                        .Replace("{cccc}", this.SecondDigits);
        }

        [ArgReviver]
        public static USPhoneNumber Revive(string key, string val)
        {
            return new USPhoneNumber(val);
        }

        private static string PhoneNumberRegex
        {
            get
            {
                string optionalDash = Group("-?");
                string openParen = Regex.Escape("(");
                string closeParen = Regex.Escape(")");

                string optionalPrefix = Group(@"(1-|1)?");                                // Phone numbers can start with 1- or 1

                string areaCodeOption1 = Group(@"[2-9]\d{2}", "areaCode");                // Area code is three digits, where the first digit cannot be 0 or 1
                string areaCodeOption2 = Group(openParen + areaCodeOption1 + closeParen); // Area code can optionally be enclosed in parentheses
                string areaCode = Group(areaCodeOption1 + "|" + areaCodeOption2);

                string firstDigitGroup = Group(@"\d{3}", "firstDigits");                  // First 3 digit grouping
                string secondDigitGroup = Group(@"\d{4}", "secondDigits");                // Second three digit grouping

                string ret = optionalPrefix + areaCode + optionalDash + firstDigitGroup + optionalDash + secondDigitGroup;

                return ret;
            }
        }
    }
}
