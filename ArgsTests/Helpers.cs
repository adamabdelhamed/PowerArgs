using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    public static class ReflectionHelper
    {
        private static T Invoke<T>(this object o, string methodName, params object[] parameters)
        {
            return (T)o.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public).Invoke(o, parameters);
        }

        private static T InvokeStatic<T>(this Type t, string methodName, params object[] parameters)
        {
            return (T)t.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, parameters);
        }
 
        public static List<string> GetShortcuts(this Type t, string propertyName)
        {
            var d = new CommandLineArgumentsDefinition(t);
            var p = d.Arguments.Where(a => a.DefaultAlias == propertyName).Single();

            var aliases = p.Aliases.ToList();
            aliases.RemoveAt(0);
            return aliases;
        }

        public static string GetShortcut(this Type t, string propertyName)
        {
            return GetShortcuts(t, propertyName).FirstOrDefault();
        }
    }

    public class UnitTestEXEAttribute : ArgHook
    {
        public override void BeforePrepareUsage(ArgHook.HookContext context)
        {
            context.Definition.ExeName = "UnitTests";
        }
    }

    public static class Helpers
    {
        public static void AssertAreEqualWithDiffInfo(string expected, string actual)
        {
            int verified = 0;

            int line = 1, col = 1;

            for (int i = 0; i < expected.Length; i++)
            {
                if (i > actual.Length - 1)
                {
                    break;
                }

                var expectedChar = expected[i];
                var actualChar = actual[i];
                if (expectedChar != actualChar)
                {
                    Assert.Fail("Character on line " + line + " and col " + col + " did not match.  Expected '" + expectedChar + ", actual '" + actualChar + "'\n\nExpected:\n"+expected+"\n\nActual:\n"+actual);
                }

                verified++;

                if (expected[i] == '\n')
                {
                    line++;
                    col = 1;
                }
                else
                {
                    col++;
                }
            }

            if (verified != expected.Length)
            {
                Assert.Fail("Verified " + verified + " characters, expected " + expected.Length + " characters");
            }
        }

        public static Action<Exception> ExpectedException<T>(string expectedText = null, bool caseSensitive =false)
        {
            return (ex) =>
            {
                Assert.IsInstanceOfType(ex, typeof(T));

                if (expectedText == null) return;

                if (caseSensitive &&
                    ex.Message.Contains(expectedText) == false)
                {
                    Assert.Fail("Error message did not contain the expected case sensitive text. Actual<{0}>. Expected<{1}>", ex.Message, expectedText);
                }
                else if (!caseSensitive &&
                    ex.Message.IndexOf(expectedText, StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    Assert.Fail("Error message did not contain the expected case insensitive text. Actual<{0}>. Expected<{1}>", ex.Message, expectedText);
                }
            };
        }

        public static Action<Exception> ExpectedArgException(string expectedText = null, bool caseSensitive = false)
        {
            return (ex) =>
            {
                var text = expectedText;
                var caseS = caseSensitive;
                if (ex is ArgException == false) Assert.Fail("An ArgException should have been thrown");

                if (caseS && text != null && !ex.Message.Contains(expectedText)) Assert.Fail("The error message did not contain the text: "+text+" - "+ex.Message);
                if (!caseS && text != null && !ex.Message.ToLower().Contains(expectedText.ToLower())) Assert.Fail("The error message did not contain the text: "+text+" - "+ex.Message);
            };
        }

        public static void Run(Action test, Action<Exception> exceptionHandler = null)
        {
            try
            {
                test.Invoke();
                if (exceptionHandler != null) Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                if (exceptionHandler != null) exceptionHandler.Invoke(ex);
                else Assert.Fail(ex.ToString());
            }
        }
    }
}
