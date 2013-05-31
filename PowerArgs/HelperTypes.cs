using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    /// <summary>
    /// An abstract class that lets you create custom argument types that match a regular expression.  The 
    /// class also makes it easy to extract named groups from the regular expression for use by your application.
    /// </summary>
    public abstract class GroupedRegexArg
    {
        /// <summary>
        /// The match that exactly matches the given regular expression
        /// </summary>
        protected Match exactMatch;

        /// <summary>
        /// Creates a new grouped regular expression argument instance.
        /// </summary>
        /// <param name="regex">The regular expression to enforce</param>
        /// <param name="input">The user input that was provided</param>
        /// <param name="errorMessage">An error message to show in the case of a non match</param>
        protected GroupedRegexArg(string regex, string input, string errorMessage)
        {
            MatchCollection matches = Regex.Matches(input, regex);
            exactMatch = (from m in matches.ToList() where m.Value == input select m).SingleOrDefault();
            if (exactMatch == null) throw new ArgException(errorMessage + ": " + input);
        }

        /// <summary>
        /// A helper function you can use to group a particular regular expression.
        /// </summary>
        /// <param name="regex">Your regular expression that you would like to put in a group.</param>
        /// <param name="groupName">The name of the group that you can use to extract the group value later.</param>
        /// <returns></returns>
        protected static string Group(string regex, string groupName = null)
        {
            return groupName == null ? "(" + regex + ")" : "(?<" + groupName + ">" + regex + ")";
        }

        /// <summary>
        /// Gets the value of the regex group from the exact match.
        /// </summary>
        /// <param name="groupName">The name of the group to lookup</param>
        /// <returns></returns>
        protected string this[string groupName]
        {
            get
            {
                return this.exactMatch.Groups[groupName].Value;
            }
        }

        /// <summary>
        /// Gets the value of the regex group from the exact match.
        /// </summary>
        /// <param name="groupNumber">The index of the group to lookup</param>
        /// <returns></returns>
        protected string this[int groupNumber]
        {
            get
            {
                return this.exactMatch.Groups[groupNumber].Value;
            }
        }
    }

    /// <summary>
    /// An example of a custom type that uses regular expressions to extract values from the command line
    /// and implements an ArgReviver to transform the text input into a complex type.
    /// This class represents a US phone number.
    /// </summary>
    public class USPhoneNumber : GroupedRegexArg
    {
        /// <summary>
        /// The three digit area code of the phone number.
        /// </summary>
        public string AreaCode { get; private set; }

        /// <summary>
        /// The three digit first segment of the phone number
        /// </summary>
        public string FirstDigits { get; private set; }

        /// <summary>
        /// The four digit second segment of the phone number.
        /// </summary>
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

        /// <summary>
        /// Gets the default string representation of the phone number in the format '1-(aaa)-bbb-cccc'.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Custom PowerArgs reviver that converts a string parameter into a custom phone number
        /// </summary>
        /// <param name="key">The name of the argument (not used)</param>
        /// <param name="val">The value specified on the command line</param>
        /// <returns>A USPhoneNumber object based on the user input</returns>
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

    internal class VirtualPropertyInfo : PropertyInfo
    {

        public override PropertyAttributes Attributes
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override Type PropertyType
        {
            get { throw new NotImplementedException(); }
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override Type DeclaringType
        {
            get { throw new NotImplementedException(); }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }
    }

    internal class VirtualNamedProperty : VirtualPropertyInfo
    {
        string name;
        Type t;
        public VirtualNamedProperty(string name, Type t)
        {
            this.name = name;
            this.t = t;
        }
         
        public override Type PropertyType
        {
            get { return t; }
        }
         

        public override string Name
        {
            get { return Name; }
        }
    }

    internal class ArgActionMethodVirtualProperty : VirtualPropertyInfo
    {
        MethodInfo method;
        public object Value { get; set; }

        public MethodInfo Method { get { return method; } }

        public ArgActionMethodVirtualProperty(MethodInfo method)
        {
            this.method = method;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            return Value;
        }

        public override Type PropertyType
        {
            get
            {
                if (method.GetParameters().Length != 1) throw new InvalidArgDefinitionException("ArgActionMethods must declare 1 parameter with the type of arguments expected by the action.");
                return method.GetParameters()[0].ParameterType;
            }
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
        {
            this.Value = value;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return new object[0];
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return new object[0];
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override string Name
        {
            get { return "Virtual_" + method.Name; }
        }
    }
}
