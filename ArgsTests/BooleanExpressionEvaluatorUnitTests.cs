using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ArgsTests
{
    [TestClass]
    public class BooleanExpressionEvaluatorUnitTests
    {
        public class ArgsWithExpression
        {
            public IBooleanExpression Expression { get; set; }
        }

        [TestMethod]
        public void TestParseExpression()
        {
            var exp = "(a | b)";
            var parsed = Args.Parse<ArgsWithExpression>("-e", exp);
            Assert.AreEqual(exp, parsed.Expression.ToString());
        }

        [TestMethod]
        public void TestParseExpressionWithQuotes()
        {
            var exp = "(\"a b c\" | b)";
            var parsed = Args.Parse<ArgsWithExpression>("-e", exp);
            Assert.AreEqual(exp, parsed.Expression.ToString());
        }


        [TestMethod]
        public void TestParseExpressionWithQuotesAndNoOperators()
        {
            var exp = "\"a b c\"";
            var parsed = Args.Parse<ArgsWithExpression>("-e", exp);
            bool eval = parsed.Expression.Evaluate(new Dictionary<string, bool>() { { "\"a b c\"", true } });
            Assert.IsTrue(eval);
            Assert.AreEqual(exp, parsed.Expression.ToString());
        }

        [TestMethod]
        public void TestParseInvalidExpression()
        {
            try
            {
                var invalidExp = "!!!!!!!!!!!(a | b)";
                var parsed = Args.Parse<ArgsWithExpression>("-e", invalidExp);
                Assert.Fail("An exception should have been thrown");
            }
            catch(ValidationArgException ex)
            {
                Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentException));
            }
        }

        [TestMethod]
        public void TestBooleanExpressionReplay()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"cake", true},
                {"eatIt", true},
            };

            var expressionText = "!(cake & eatIt)";
            var expression = BooleanExpressionParser.Parse(expressionText);
            Assert.IsFalse(expression.Evaluate(variableValues));
            Assert.AreEqual(expressionText, expression.ToString());
        }

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
        public void TestBooleanExpressionsSimpleNot()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
            };
            var parseResult = BooleanExpressionParser.Parse("!a|bb");
            Assert.IsTrue(parseResult.Evaluate(variableValues));

            variableValues["a"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));
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
        public void TestBooleanExpressionsGroupsWithNot()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
            };
            var parseResult = BooleanExpressionParser.Parse("!(a)&!(bb)");
            Assert.IsTrue(parseResult.Evaluate(variableValues));

            variableValues["a"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["bb"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));
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

        [TestMethod]
        public void TestBooleanExpressionsComplexGroupsWithNots()
        {
            Dictionary<string, bool> variableValues = new Dictionary<string, bool>
            {
                {"a", false},
                {"bb", false},
                {"c", false},
            };
            var parseResult = BooleanExpressionParser.Parse("!( ((a)&(bb)) | c )");
            Assert.IsTrue(parseResult.Evaluate(variableValues));

            variableValues["c"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));
            variableValues["c"] = false;


            variableValues["a"] = true;
            Assert.IsTrue(parseResult.Evaluate(variableValues));

            variableValues["bb"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));

            variableValues["c"] = true;
            Assert.IsFalse(parseResult.Evaluate(variableValues));
        }


        [TestMethod]
        public void TestBooleanExpressionsGroupLeftOpen()
        {
            try
            {
                BooleanExpressionParser.Parse("( (a)&(bb ) | c");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You are missing at least 1 closing parenthesis", ex.Message);
            }
        }

        [TestMethod]
        public void TestBooleanExpressionsGroupTooManyCloses()
        {
            try
            {
                BooleanExpressionParser.Parse(" (a)&(bb) ) | c");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("Unexpected token ')'", ex.Message);
            }
        }


        [TestMethod]
        public void TestBooleanExpressionsEmptyGroup()
        {
            try
            {
                BooleanExpressionParser.Parse("(a)|()");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You have an empty set of parenthesis", ex.Message);
            }
        }

        [TestMethod]
        public void TestBooleanExpressionsWrongOperator()
        {
            try
            {
                var parsed = BooleanExpressionParser.Parse("(a)||(b)");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You cannot have two consecutive operators '&&' or '||', use '&' or '|'", ex.Message);
            }

            try
            {
                var parsed = BooleanExpressionParser.Parse("(a && b)");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You cannot have two consecutive operators '&&' or '||', use '&' or '|'", ex.Message);
            }

            try
            {
                var parsed = BooleanExpressionParser.Parse("(a & & b)");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You cannot have two consecutive operators '&&' or '||', use '&' or '|'", ex.Message);
            }
        }

        [TestMethod]
        public void TestBooleanExpressionsConsecutiveNots()
        {
            try
            {
                BooleanExpressionParser.Parse("!!a");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You cannot have two consecutive '!' operators", ex.Message);
            }
        }

        [TestMethod]
        public void TestBooleanExpressionsTrailingNot()
        {
            try
            {
                BooleanExpressionParser.Parse("a!");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("Unexpected token '!' at end of expression", ex.Message);
            }
        }

        [TestMethod]
        public void TestBooleanExpressionsNotThenBooleanOperator()
        {
            try
            {
                BooleanExpressionParser.Parse("a!&b");
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual("You cannot have an operator '&' or '|' after a '!'", ex.Message);
            }
        }
    }
}
