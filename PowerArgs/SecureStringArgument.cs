using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;

namespace PowerArgs
{
    public class SecureStringArgument
    {
        string name;
        internal SecureStringArgument(string name)
        {
            this.name = name;
        }

        SecureString secureString;
        public SecureString SecureString
        {
            get
            {
                if (secureString != null) return secureString;

                Console.Write("Enter value for " + name + ": ");
                SecureString ret = new SecureString();
                int index = 0;
                while (true)
                {
                    var key = ConsoleHelper.ConsoleImpl.ReadKey();
                    if (key.Key == ConsoleKey.Enter) break;

                    if (key.Key == ConsoleKey.Backspace && index > 0) ret.RemoveAt(--index);
                    else if (key.Key == ConsoleKey.Delete && index < ret.Length) ret.RemoveAt(index);
                    else if (key.Key == ConsoleKey.LeftArrow) index = Math.Max(0, index - 1);
                    else if (key.Key == ConsoleKey.RightArrow) index = Math.Min(ret.Length - 1, index++);
                    else if (key.Key == ConsoleKey.Home) index = 0;
                    else if (key.Key == ConsoleKey.End) index = ret.Length;

                    else if (char.IsLetterOrDigit(key.KeyChar) || key.KeyChar == ' ' || char.IsSymbol(key.KeyChar) || char.IsPunctuation(key.KeyChar))
                    {
                        if (index < ret.Length)
                        {
                            ret.InsertAt(index, key.KeyChar);
                        }
                        else
                        {
                            ret.AppendChar(key.KeyChar);
                        }
                        index++;
                    }
                }

                ret.MakeReadOnly();
                secureString = ret;
                return ret;
            }
        }

        public string ConvertToNonsecureString()
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(this.SecureString);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
