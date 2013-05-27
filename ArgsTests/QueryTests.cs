using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class QueryTests
    {
        public class DataSource
        {
            public List<int> Numbers{get;set;}
            public DataSource()
            {
                   Numbers = new List<int>() { 5, 4, 3, 2, 1, 6, 7, 8, 9, 10 };
            }
        }

        public class TestArgs
        {
            public string OrderBy { get; set; }
            [ArgShortcut("o-")]
            public string OrderByDescending { get; set; }
            public string Where { get; set; }
            public int Skip { get; set; }
            public int Take { get; set; }

            [Query(typeof(DataSource))]
            [ArgIgnore]
            public List<int> Numbers { get; set; }
        }

        [TestMethod]
        public void QueryArgsThrowsOnUncompilableQuery()
        {
            try
            {
                Args.Parse<TestArgs>("-Where", "foobar == 'JJ'");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(QueryInvalidArgException));
                Assert.AreEqual("Could not compile your query", ex.Message);
            }
        }

        [TestMethod]
        public void TestQuerySkipTake()
        {
            var parsed = Args.Parse<TestArgs>("-Skip", "5", "-Take", "5");
            Assert.AreEqual(5, parsed.Numbers.Count);
            Assert.AreEqual(6, parsed.Numbers[0]);
            Assert.AreEqual(10, parsed.Numbers[4]);
        }

        [TestMethod]
        public void TestQueryWhere()
        {
            var parsed = Args.Parse<TestArgs>("-Where", "item == 7");
            Assert.AreEqual(1, parsed.Numbers.Count);
            Assert.AreEqual(7, parsed.Numbers[0]);
        }

        [TestMethod]
        public void TestQueryOrderBy()
        {
            var parsed = Args.Parse<TestArgs>("-OrderBy", "item");

            Assert.AreEqual(10, parsed.Numbers.Count);
            for (int i = 1; i <= 10; i++)
            {
                Assert.AreEqual(i, parsed.Numbers[i - 1]);
            }
        }

        [TestMethod]
        public void TestQueryOrderByDescending()
        {
            var parsed = Args.Parse<TestArgs>("-OrderByDescending", "item");

            Assert.AreEqual(10, parsed.Numbers.Count);
            for (int i = 10; i > 0; i--)
            {
                Assert.AreEqual(i, parsed.Numbers[10-i]);
            }
        }
    }
}
