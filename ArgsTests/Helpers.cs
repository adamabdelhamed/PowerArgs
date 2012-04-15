using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    public class ExpectedException
    {
        public Type Type { get; set; }
        public string Contains { get; set; }
        public bool CaseSensitive { get; set; }

        public ExpectedException(Type t)
        {
            this.Type = t;
        }

        public static bool Expects(ExpectedException expected,  Exception ex)
        {
            if (expected == null) return false;
            if (ex.GetType() != expected.Type) return false;

            if (expected.Contains != null)
            {
                if (expected.CaseSensitive)
                {
                    if (ex.Message.Contains(expected.Contains) == false) return false;
                }
                else
                {
                    if (ex.Message.ToLower().Contains(expected.Contains.ToLower()) == false) return false;
                }
            }

            return true;
        }
    }

    public static class Helpers
    {
        public static void Run(Action test, ExpectedException expectedException = null)
        {
            try
            {
                test.Invoke();
                if (expectedException != null) Assert.Fail("An exception of type '"+expectedException.Type.Name+"' should have been thrown");
            }
            catch (TargetInvocationException ex)
            {
                if (ExpectedException.Expects(expectedException, ex.InnerException)) return;
                Assert.Fail(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                if (ExpectedException.Expects(expectedException, ex)) return;
                Assert.Fail(ex.ToString());
            }
        }
    }
}
