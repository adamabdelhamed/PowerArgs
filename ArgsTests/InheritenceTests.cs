using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
namespace ArgsTests
{
    [TestClass]
    public class InheritenceTests
    {
        public class BaseArgs
        {
            public string StringArg { get; set; }
        }

        public class DerivedArgs : BaseArgs { }

        public class ActionWithDerivedArgs
        {
            public static event Action DoCalled;

            [ArgActionMethod]
            public void Do(DerivedArgs args)
            {
                Assert.AreEqual("teststring", args.StringArg);
                if (DoCalled != null) DoCalled();
            }
        }

        [TestMethod]
        public void TestDerivedActionParams()
        {
            bool fired = false;
            Action handler = () => { fired = true;  };

            try
            {
                ActionWithDerivedArgs.DoCalled+= handler;
                var result = Args.InvokeAction<ActionWithDerivedArgs>("do", "-s", "teststring");
                Assert.IsInstanceOfType(result.ActionArgs, typeof(DerivedArgs));
                Assert.AreEqual("teststring", ((DerivedArgs)result.ActionArgs).StringArg);
                Assert.IsTrue(fired);
            }
            finally
            {
                ActionWithDerivedArgs.DoCalled-=handler;
            }
        }
    }
}
