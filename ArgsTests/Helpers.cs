using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    public static class Helpers
    {
        public static Action<Exception> ExpectedArgException(string expectedText = null, bool caseSensitive = false)
        {
            return (ex) =>
            {
                var text = expectedText;
                var caseS = caseSensitive;
                if (ex is ArgException == false) Assert.Fail("An ArgException should have been thrown");

                if (caseS && text != null && !ex.Message.Contains(expectedText)) Assert.Fail("The error message did not contain the text: "+text+" - "+ex.Message);
                if (!caseS && text != null && !ex.Message.ToLower().Contains(expectedText)) Assert.Fail("The error message did not contain the text: "+text+" - "+ex.Message);
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
