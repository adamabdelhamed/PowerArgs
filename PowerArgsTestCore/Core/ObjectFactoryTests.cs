using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class ObjectFactoryTests
    {
        public class Foo
        {
            public string SomeProp { get; set; }
        }

        [TestMethod]
        public void TestObjectFactory()
        {
            try
            {
                Args.RegisterFactory(typeof(Foo), () => new Foo() { SomeProp = "FooVal" });
                var foo = Args.Parse<Foo>();
                Assert.AreEqual("FooVal", foo.SomeProp);
            }
            finally
            {
                Args.UnRegisterFactory(typeof(Foo));
                var foo = Args.Parse<Foo>();
                Assert.AreEqual(null, foo.SomeProp);
            }
        }
    }
}
