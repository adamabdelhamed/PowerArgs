using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests
{
    [TestClass]
    public class BooleanExpressionEvaluatorUnitTests
    {
        [TestMethod]
        public void TestBooleanExpressionsSimpleOr()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
            };
            var parseResult = BooleanExpressionParser.Parse("a|bb");
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["a"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));
        }

        [TestMethod]
        public void TestBooleanExpressionsSimpleAnd()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
            };
            var parseResult = BooleanExpressionParser.Parse("a&bb");
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["a"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["bb"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));
        }

        [TestMethod]
        public void TestBooleanExpressionsSimpleGroups()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
            };
            var parseResult = BooleanExpressionParser.Parse("(a)&(bb)");
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["a"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["bb"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));
        }

        [TestMethod]
        public void TestBooleanExpressionsComplexGroups()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
                {"c", false},
            };
            var parseResult = BooleanExpressionParser.Parse("( (a)&(bb) ) | c");
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["c"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));
            variableValues["c"] = false;


            variableValues["a"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["bb"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));

            variableValues["c"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));
        }
    }
}
