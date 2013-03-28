using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Linq;
namespace ArgsTests
{
    [TestClass]
    public class ConsoleStringTests
    {
        [TestMethod]
        public void TestBasicConsoleString()
        {
            ConsoleString val = new ConsoleString("Adam");
            val += " Abdelhamed";

            ValidateStringCharacteristics("Adam Abdelhamed", val);
        }

        [TestMethod]
        public void TestMultiSegmentConsoleString()
        {
            ConsoleString val = new ConsoleString("Adam", ConsoleColor.Red);
            val += new ConsoleString(" M", ConsoleColor.White);
            val += new ConsoleString("", ConsoleColor.Black);
            val += new ConsoleString(" Abdelhamed", ConsoleColor.Blue);

            ValidateStringCharacteristics("Adam M Abdelhamed", val);
            val.Write();
        }

        [TestMethod]
        public void TestMultiSegmentConsoleStringEqualityPositive()
        {
            ConsoleString[] equal = new ConsoleString[2];
            for (int i = 0; i < equal.Length; i++)
            {
                equal[i] = new ConsoleString("Adam", ConsoleColor.Red);
                equal[i] += new ConsoleString(" M", ConsoleColor.White);
                equal[i] += new ConsoleString("", ConsoleColor.Black);
                equal[i] += new ConsoleString(" Abdelhamed", ConsoleColor.Blue);
            }

            Assert.IsTrue(equal[0].Equals(equal[1]));
            Assert.IsTrue(equal[1].Equals(equal[0]));
            Assert.IsFalse(equal[0].Equals(null));
            Assert.IsFalse(equal[0].Equals(10));
        }

        [TestMethod]
        public void TestMultiSegmentConsoleStringEqualityNegative()
        {
            ConsoleString[] equal = new ConsoleString[2];
            for (int i = 0; i < equal.Length; i++)
            {
                equal[i] = new ConsoleString("Adam", ConsoleColor.Red);
                equal[i] += new ConsoleString(" M", i== 0 ? ConsoleColor.White : ConsoleColor.Black);
                equal[i] += new ConsoleString("", ConsoleColor.Black);
                equal[i] += new ConsoleString(" Abdelhamed", ConsoleColor.Blue);
            }

            Assert.IsFalse(equal[0].Equals(equal[1]));
            Assert.IsFalse(equal[1].Equals(equal[0]));


            ConsoleString a = new ConsoleString("Ada");
            ConsoleString b = new ConsoleString("Adam");

            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(b.Equals(a));
        }

        [TestMethod]
        public void TestConsoleStringEdgeCases()
        {
            ConsoleString str = ConsoleString.Empty;
            for(int i = 0; i < 99; i++) str.Append("");

            Assert.AreEqual(0, str.Length);
            Assert.AreEqual(string.Empty, str.ToString());
            Assert.AreEqual(ConsoleString.Empty, str);

            ConsoleString noSegments = new ConsoleString();
            noSegments.AppendUsingCurrentFormat("Adam");
            ValidateStringCharacteristics("Adam", noSegments);

            ConsoleString nullString = null;
            Assert.IsTrue(nullString == null);
            nullString = nullString + new ConsoleString("Adam");
            Assert.AreEqual(nullString, new ConsoleString("Adam"));


            nullString = null;
            Assert.IsTrue(nullString == null);
            Assert.IsFalse(nullString != null);
            nullString = nullString + "Adam";
            Assert.AreEqual(nullString, new ConsoleString("Adam"));
            Assert.IsTrue(nullString != null);
            Assert.IsFalse(new ConsoleCharacter('a').Equals(null));
            Assert.IsFalse(new ConsoleCharacter('a').Equals(0));

            new ConsoleCharacter('a').GetHashCode();
            new ConsoleString("Adam").GetHashCode();

            Assert.IsTrue(new ConsoleString("Adam").Equals("Adam"));
            Assert.IsTrue(new ConsoleCharacter('A').Equals('A'));

            Assert.IsTrue(new ConsoleCharacter('A') == 'A');
            Assert.IsTrue(new ConsoleCharacter('A') != 'B');
            Assert.IsFalse(new ConsoleCharacter('A') == null);
            Assert.IsTrue(new ConsoleCharacter('A') != null);

            Assert.IsTrue(new ConsoleCharacter('A') == new ConsoleCharacter('A'));
            Assert.IsTrue(new ConsoleCharacter('A') != new ConsoleCharacter('B'));

            Assert.IsTrue(new ConsoleString("A") == new ConsoleString("A"));
            Assert.IsTrue(new ConsoleString("A") != new ConsoleString("B"));

            Assert.IsFalse(null == new ConsoleString("A"));
            Assert.IsTrue(null != new ConsoleString("B"));

            Assert.IsFalse(new ConsoleString("A") == null);
            Assert.IsTrue(new ConsoleString("A") != null);

            Assert.AreEqual(new ConsoleString("A"), null + new ConsoleString("A"));


            ConsoleString nulla = null;
            ConsoleString nullb = null;
            string nullS = null;

            Assert.AreEqual(null, nulla + nullb);
            Assert.AreEqual(null, nulla + nullS);
        }


        private static void ValidateStringCharacteristics(string expected, ConsoleString actual)
        {
            Assert.AreEqual(expected, string.Join("", actual.Select(c => c.Value)));
            Assert.AreEqual(expected, actual.ToString());
            Assert.AreEqual(expected.Length, actual.Length);

            var expectedEnumerator = expected.GetEnumerator();
            foreach (var character in actual)
            {
                expectedEnumerator.MoveNext();
                Assert.AreEqual(expectedEnumerator.Current+"", character.ToString());
                Assert.AreEqual(expectedEnumerator.Current, character.Value);
                character.Write();
            }

            Assert.IsFalse(expectedEnumerator.MoveNext());
        }
    }
}
