using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests.Templating
{
    [TestClass]
    public class ObjectPathExpressionTests
    {
        [TestMethod]
        public void TestObjectPathExpressionSingleProperty()
        {
            var exp = ObjectPathExpression.Parse("Length");
            var eval = exp.Evaluate("1234");
            Assert.AreEqual(4, eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionNestedProperty()
        {
            var exp = ObjectPathExpression.Parse("Person.FirstName");
            var eval = exp.Evaluate(new { Person = new { FirstName = "John", LastName = "Smith" } });
            Assert.AreEqual("John", eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionStringIndexer()
        {
            var exp = ObjectPathExpression.Parse("[0]");
            var eval = exp.Evaluate("ABCD");
            Assert.AreEqual('A', eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionArrayIndexer()
        {
            var exp = ObjectPathExpression.Parse("[0]");
            var eval = exp.Evaluate(new int[] { 100, 200, 300 });
            Assert.AreEqual(100, eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionListIndexer()
        {
            var exp = ObjectPathExpression.Parse("[0]");
            var eval = exp.Evaluate(new List<int>{ 100, 200, 300 });
            Assert.AreEqual(100, eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionDictionaryIndexer()
        {
            var exp = ObjectPathExpression.Parse("[\"MeaningOfLife\"]");
            var eval = exp.Evaluate(new Dictionary<string, int>() { { "MeaningOfLife", 42 } });
            Assert.AreEqual(42, eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionDictionaryIndexerWithSpaces()
        {
            var exp = ObjectPathExpression.Parse("[\"Meaning Of Life\"]");
            var eval = exp.Evaluate(new Dictionary<string, int>() { { "Meaning Of Life", 42 } });
            Assert.AreEqual(42, eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionNestedPropertyAfterIndexer()
        {
            var exp = ObjectPathExpression.Parse("[0].Person.FirstName");
            var eval = exp.Evaluate(new object[] 
            { 
                new { Person = new { FirstName = "John", LastName = "Smith" } } ,
                new { Person = new { FirstName = "Bob", LastName = "Smith" } } 
            });
            Assert.AreEqual("John", eval);
        }

        [TestMethod]
        public void TestObjectPathExpressionNestedPropertyAfterIndexerWithWhitespaceEverywhere()
        {
            var exp = ObjectPathExpression.Parse("    [   0    ]     .          Person  .   FirstName   ");
            var eval = exp.Evaluate(new object[] 
            { 
                new { Person = new { FirstName = "John", LastName = "Smith" } } ,
                new { Person = new { FirstName = "Bob", LastName = "Smith" } } 
            });
            Assert.AreEqual("John", eval);
        }
    }
}
