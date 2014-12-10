using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Reflection;
using System.Linq;
namespace ArgsTests
{
    [TestClass]
    public class ComposableArgActionsTests
    {

        public class UnitTestArgActionResolver : ArgActionResolver
        {
            public override System.Collections.Generic.IEnumerable<Type> ResolveActionTypes()
            {
                return Assembly.GetExecutingAssembly().GetTypes().Where(t => t.HasAttr<ArgActions>() && t.Name.Contains("ComposableActions_"));
            }
        }

        [UnitTestArgActionResolver]
        public class Coordinator
        {

        }

        public class Action1Exception : Exception { }

        [ArgActions]
        public class ComposableActions_1
        {
            [ArgActionMethod]
            public static void Action1() 
            {
                throw new Action1Exception();
            }

            [ArgActionMethod]
            public static void Action2() { }
        }

        [ArgActions]
        public class ComposableActions_2
        {
            [ArgActionMethod]
            public static void Action3() { }
        }

        [TestMethod]
        public void TestComposableActionsBasic()
        {
            var action = Args.ParseAction<Coordinator>("Action1");
            Assert.AreEqual(3, action.Definition.Actions.Count);
            Assert.AreEqual("Action1", action.Definition.Actions[0].DefaultAlias);
            Assert.AreEqual("Action2", action.Definition.Actions[1].DefaultAlias);
            Assert.AreEqual("Action3", action.Definition.Actions[2].DefaultAlias);
            try
            {
                action.Invoke();
                Assert.Fail("An exception should have been thrown");
            }
            catch(Action1Exception)
            {

            }
        }
    }
}
