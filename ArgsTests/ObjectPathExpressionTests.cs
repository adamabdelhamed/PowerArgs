using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
    public class ObjectPathExpressionTests
    {
        public class Customer
        {
            public string Name { get; set; }
            public Address Address { get; set; } = new Address();

            public List<Customer> RelatedCustomers { get; private set; } = new List<Customer>();
        }

        public class Address
        {
            public string Line1 { get; set; }
            public string Line2 { get; set; }
            public string ZipCode { get; set; }
        }

        [TestMethod]
        public void TestOneLevel()
        {
            var testString = "Adam";
            var expression = ObjectPathExpression.Parse("Length");
            var eval = expression.Evaluate(testString);
            Assert.AreEqual(testString.Length, eval);
        }

        [TestMethod]
        public void TestTwoLevels()
        {
            var testObj = new Customer() { Name = "Adam", Address = new Address() { Line1 = "123 Main Street", Line2 = "Seattle, WA", ZipCode = "12345" } };
            var expression = ObjectPathExpression.Parse("Address.Line1");
            var eval = expression.Evaluate(testObj);
            Assert.AreEqual(testObj.Address.Line1, eval);
        }

        [TestMethod]
        public void TestNumericIndexers()
        {
            var testObj = new Customer() { Name = "Adam", Address = new Address() { Line1 = "123 Main Street", Line2 = "Seattle, WA", ZipCode = "12345" } };
            testObj.RelatedCustomers.Add(testObj);
            var expression = ObjectPathExpression.Parse("RelatedCustomers[0]");
            var eval = expression.Evaluate(testObj);
            Assert.AreEqual(testObj.RelatedCustomers[0], eval);
        }

        [TestMethod]
        public void TestDeep()
        {
            var testObj = new Customer() { Name = "Adam", Address = new Address() { Line1 = "123 Main Street", Line2 = "Seattle, WA", ZipCode = "12345" } };
            testObj.RelatedCustomers.Add(testObj);
            var expression = ObjectPathExpression.Parse("RelatedCustomers[0].RelatedCustomers[0].Name");
            var eval = expression.Evaluate(testObj);
            Assert.AreEqual(testObj.RelatedCustomers[0].RelatedCustomers[0].Name, eval);
        }


        [TestMethod]
        public void TestStartWithIndexer()
        {
            var testObj = new string[1][];
            testObj[0] = new string[1];
            testObj[0][0] = "Adam";
 
            var expression = ObjectPathExpression.Parse("[0]");
            var eval = expression.Evaluate(testObj);
            Assert.AreEqual(testObj[0], eval);
        }

        [TestMethod]
        public void TestBackToBackIndexer()
        {
            var testObj = new string[1][];
            testObj[0] = new string[1];
            testObj[0][0] = "Adam";

            var expression = ObjectPathExpression.Parse("[0][0]");
            var eval = expression.Evaluate(testObj);
            Assert.AreEqual(testObj[0][0], eval);
        }
    }
}
