using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsTests
{
    [TestClass]
    public class MultipleFactoredOutActionsTests
    {
        public static bool Barked = false;
        public static bool Meowed = false;

        [ArgActionType(typeof(DogActions))]
        [ArgActionType(typeof(CatActions))]
        public class DeferredActionsArgs
        {

        }

        public class DogActions
        {
            [ArgActionMethod]
            public static void Bark()
            {
                MultipleFactoredOutActionsTests.Barked = true;
            }
        }

        public class CatActions
        {
            [ArgActionMethod]
            public static void Meow()
            {
                MultipleFactoredOutActionsTests.Meowed = true;
            }
        }

        [TestMethod]
        public void TestMultipleDeferredActionClasses()
        {
            MultipleFactoredOutActionsTests.Barked = false;
            MultipleFactoredOutActionsTests.Meowed = false;

            Args.InvokeAction<DeferredActionsArgs>("bark");
            Assert.IsTrue(MultipleFactoredOutActionsTests.Barked);

            Args.InvokeAction<DeferredActionsArgs>("meow");
            Assert.IsTrue(MultipleFactoredOutActionsTests.Meowed);
        }
    }
}
