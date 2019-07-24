using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class DisplayNameTests
    {
        public class PropertyDisplayNameTestsArgs
        {
            [ArgDisplayName("DisplayName")]
            public string Property { get; set; }
        }

        public class MethodDisplayNameTestsArgs
        {
            [ArgActionMethod, ArgDescription("Adds the two operands"), ArgDisplayName("Add")]
            public void MyAddOperation(TwoOperandArgs args)
            {
                sum = args.Value1 + args.Value2;
            }

            public static int sum { get; set; } = 0;
        }

        public class TwoOperandArgs
        {
            [ArgRequired, ArgDescription("The first operand to process"), ArgPosition(1)]
            public int Value1 { get; set; }
            [ArgRequired, ArgDescription("The second operand to process"), ArgPosition(2)]
            public int Value2 { get; set; }
        }

        [TestMethod]
        public void TestPropertyDisplayName()
        {
            var usage = ArgUsage.GenerateUsageFromTemplate(typeof(PropertyDisplayNameTestsArgs));
            Assert.IsTrue(usage.Contains("DisplayName"));
            Assert.IsTrue(usage.Contains("-D"));
        }

        [TestMethod]
        public void TestMethodDisplayName()
        {
            var usage = ArgUsage.GenerateUsageFromTemplate(typeof(MethodDisplayNameTestsArgs));
            Assert.IsTrue(usage.Contains("Add"));
            Assert.IsTrue(!usage.Contains("MyAddOperation"));

            var args = new string[] { "Add", "1", "2" };
            var initSum = MethodDisplayNameTestsArgs.sum;
            var parsed = Args.InvokeAction<MethodDisplayNameTestsArgs>(args);
            Assert.IsTrue(initSum == 0);
            Assert.AreEqual(3, MethodDisplayNameTestsArgs.sum);
        }
    }
}
