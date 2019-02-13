﻿using System;
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
        public void TestConsoleStringWriteLine()
        {
            var existingProvider = ConsoleString.ConsoleProvider;
            try
            {
                var testProvider = new TestConsoleProvider();
                ConsoleString.ConsoleProvider = testProvider;
                ConsoleString str = new ConsoleString("Adam");

                bool confirmed = false;

                string written = "";
                string target = "Adam" + Environment.NewLine;
                testProvider.WriteHappened += (s) =>
                {
                    written += s;
                    if (written == target)
                    {
                        confirmed = true;
                    }
                    else
                    {
                        Assert.IsFalse(written.StartsWith(target), "Extra characters after target: '" + written + "'");
                    }

                };

                str.WriteLine();
                Assert.IsTrue(confirmed);
            }
            finally
            {
                ConsoleString.ConsoleProvider = existingProvider;
            }
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
        public void TestReplaceCharByChar()
        {
            var testString = "Some Test String";

            for (int i = 0; i < testString.Length; i++)
            {
                ConsoleString orig = new ConsoleString(testString);
                ConsoleString replaced = orig.Replace(testString[i]+"", testString[i]+"", ConsoleColor.Red);

                Assert.AreEqual(ConsoleColor.Gray, orig[i].ForegroundColor);
                Assert.AreEqual(ConsoleColor.Red, replaced[i].ForegroundColor);
            }
        }

        [TestMethod]
        public void TestReplaceOtherCases()
        {
            ConsoleString orig = new ConsoleString("RedWBlue");
            ConsoleString white = orig.Replace("W", "White", ConsoleColor.White);

            Assert.AreEqual("RedWBlue", orig.ToString());
            Assert.AreEqual("RedWhiteBlue", white.ToString());
            Assert.AreEqual("White", string.Join("",white.Where(c => c.ForegroundColor == ConsoleColor.White).Select(c=> c.Value)));
        }

        [TestMethod]
        public void TestIndexOf()
        {
            ConsoleString s = new ConsoleString("0123456789");

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, s.IndexOf(i + ""));
            }

            Assert.AreEqual(0, s.IndexOf("0123456789"));
            Assert.AreEqual(-1, s.IndexOf("01234567890"));
            Assert.AreEqual(0, s.IndexOf(""));
            Assert.AreEqual(-1, s.IndexOf("A"));
            Assert.AreEqual(-1, s.IndexOf(null as string));
            Assert.AreEqual(0, s.IndexOf("01"));
            Assert.AreEqual(1, s.IndexOf("12"));
            Assert.AreEqual(8, s.IndexOf("89"));

            Assert.AreEqual(0, s.IndexOf(s));
            Assert.AreEqual(-1, s.IndexOf(s.ToString().ToRed()));

            for (int i = 0; i < 1000; i++)
            {
                s += "-";
            }

            s += "!";

           Assert.AreEqual(1010,s.IndexOf("!"));
        }

        [TestMethod]
        public void TestIndexOfCustomComparison()
        {
            Assert.AreEqual(-1, new ConsoleString("Adam").IndexOf("adam", StringComparison.InvariantCulture));
            Assert.AreEqual(0, new ConsoleString("Adam").IndexOf("adam", StringComparison.InvariantCultureIgnoreCase));
        }

        [TestMethod]
        public void TestContainsCustomComparison()
        {
            Assert.IsFalse(new ConsoleString("Adam").Contains("adam", StringComparison.InvariantCulture));
            Assert.IsTrue(new ConsoleString("Adam").Contains("adam", StringComparison.InvariantCultureIgnoreCase));
        }

        [TestMethod]
        public void TestReplaceCustomComparison()
        {
            Assert.AreEqual(new ConsoleString("Adam"), new ConsoleString("adam").Replace("adam", "Adam", comparison: StringComparison.InvariantCulture));
        }

        [TestMethod]
        public void TestHighlightCustomComparison()
        {
            var highlighted = new ConsoleString("Adam").Highlight("a",foregroundColor: ConsoleColor.Red, comparison: StringComparison.InvariantCultureIgnoreCase);
            Assert.AreEqual(highlighted[0], new ConsoleCharacter('A', ConsoleColor.Red));
            Assert.AreEqual(highlighted[1], new ConsoleCharacter('d'));
            Assert.AreEqual(highlighted[2], new ConsoleCharacter('a', ConsoleColor.Red));
            Assert.AreEqual(highlighted[3], new ConsoleCharacter('m'));

            Assert.AreEqual("The quick brown fox", new ConsoleString("The quick brown fox").Highlight("brown", ConsoleColor.Red).ToString());
        }

        [TestMethod]
        public void TestContains()
        {
            ConsoleString s = new ConsoleString("0123456789");
            Assert.IsTrue(s.Contains("2345"));
            Assert.IsTrue(s.Contains("0"));
            Assert.IsTrue(s.Contains("01"));
            Assert.IsTrue(s.Contains("9"));
            Assert.IsTrue(s.Contains("89"));

            Assert.IsFalse(s.Contains("A"));
            Assert.IsFalse(s.Contains("0123A"));
        }

        [TestMethod]
        public void TestSubstring()
        {
            ConsoleString orig = new ConsoleString("0123456789");
            ConsoleString sub = orig.Substring(5);
            ConsoleString sub2 = orig.Substring(5,1);
            Assert.AreEqual("56789", sub.ToString());
            Assert.AreEqual("5", sub2.ToString());
        }

        [TestMethod]
        public void TestReplaceMultiple()
        {
            ConsoleString orig = new ConsoleString("WRedWBlueW");
            ConsoleString white = orig.Replace("W", "White", ConsoleColor.White);

            Assert.AreEqual("WRedWBlueW", orig.ToString());
            Assert.AreEqual("WhiteRedWhiteBlueWhite", white.ToString());
            Assert.AreEqual("WhiteWhiteWhite", string.Join("", white.Where(c => c.ForegroundColor == ConsoleColor.White).Select(c => c.Value)));
        }

        [TestMethod]
        public void TestReplaceRegex()
        {
            ConsoleString orig = new ConsoleString("Credit Card: 1234-5678-9876-5432 - VISA");
            ConsoleString cleaned = orig.ReplaceRegex(@"\d\d\d\d-\d\d\d\d-\d\d\d\d-\d\d\d\d", "xxxx-xxxx-xxxx-xxxx", ConsoleColor.White);
            Assert.AreEqual("Credit Card: xxxx-xxxx-xxxx-xxxx - VISA", cleaned.ToString());

            ConsoleString hasPhoneNumber = new ConsoleString("Number: 222-333-4444");
            hasPhoneNumber = hasPhoneNumber.ReplaceRegex(@"\d{3}-\d{3}-\d{4}", null, ConsoleColor.Green);

            Assert.AreEqual("Number: 222-333-4444", hasPhoneNumber.ToString());
            Assert.AreEqual(new ConsoleString("222-333-4444", ConsoleColor.Green), hasPhoneNumber.Substring(8));
        }

        [TestMethod]
        public void TestStartsWithAndEndsWith()
        {
            Assert.IsTrue(new ConsoleString("12345").StartsWith("123"));
            Assert.IsFalse(new ConsoleString("12345").StartsWith("0123"));
            Assert.IsFalse(new ConsoleString("12345").StartsWith("01231111111111111111111"));


            Assert.IsTrue(new ConsoleString("12345").EndsWith("345"));
            Assert.IsFalse(new ConsoleString("12345").EndsWith("234"));
            Assert.IsFalse(new ConsoleString("12345").EndsWith("01231111111111111111111"));
        }

        [TestMethod]
        public void TestConsoleStringEdgeCases()
        {
            ConsoleString str = ConsoleString.Empty;
            for(int i = 0; i < 99; i++) str+=("");

            Assert.AreEqual(0, str.Length);
            Assert.AreEqual(string.Empty, str.ToString());
            Assert.AreEqual(ConsoleString.Empty, str);

            ConsoleString noSegments = new ConsoleString();
            noSegments = noSegments.AppendUsingCurrentFormat("Adam");
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

        [TestMethod]
        public void TestConsoleStringHelpers()
        {
            foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor)))
            {
                var method = typeof(StringEx).GetMethod("To" + color, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                Assert.AreEqual(new ConsoleString("Hello", color, color), method.Invoke(null,new object[] { "Hello", color }));
            }

            foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor)))
            {
                ConsoleString baseString = new ConsoleString("Hello", null, null);
                var method = typeof(ConsoleString).GetMethod("To" + color);
                Assert.AreEqual(new ConsoleString(baseString.ToString(), color, color), method.Invoke(baseString, new object[] { color }));
            }
        }

        [TestMethod]
        public void TestConsoleCharacterHelpers()
        {
            foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor)))
            {
                var method = typeof(ConsoleCharacter).GetMethod(color+"", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                Assert.AreEqual(new ConsoleCharacter('*', color, color), method.Invoke(null, new object[] { '*', color }));
                Assert.AreEqual(new ConsoleCharacter('*', color, null), method.Invoke(null, new object[] { '*', null }));
            }

            foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor)))
            {
                var method = typeof(ConsoleCharacter).GetMethod(color + "BG", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                Assert.AreEqual(new ConsoleCharacter('*', color, color), method.Invoke(null, new object[] { '*', color }));
                Assert.AreEqual(new ConsoleCharacter('*', null, color), method.Invoke(null, new object[] { '*', null }));
            }
        }

        [TestMethod]
        public void TestConvertBetweenConsoleBitmapAndConsoleStringTrimMode()
        {
            // the last 4 characters will be whitespace with default formatting
            var str = "Adam".ToYellow(bg: ConsoleColor.Green) + "    ".ToConsoleString();
            var bmp = str.ToConsoleBitmap();
            Assert.AreEqual(8, bmp.Width);
            Assert.AreEqual(1, bmp.Height);
            Assert.AreEqual('A', bmp.GetPixel(0, 0).Value.Value.Value);
            Assert.AreEqual('d', bmp.GetPixel(1, 0).Value.Value.Value);
            Assert.AreEqual('a', bmp.GetPixel(2, 0).Value.Value.Value);
            Assert.AreEqual('m', bmp.GetPixel(3, 0).Value.Value.Value);
            Assert.AreEqual(' ', bmp.GetPixel(4, 0).Value.Value.Value);
            Assert.AreEqual(' ', bmp.GetPixel(5, 0).Value.Value.Value);
            Assert.AreEqual(' ', bmp.GetPixel(6, 0).Value.Value.Value);
            Assert.AreEqual(' ', bmp.GetPixel(7, 0).Value.Value.Value);

            for (var x = 0; x < "Adam".Length; x++)
            {
                Assert.AreEqual(ConsoleColor.Yellow, bmp.GetPixel(x, 0).Value.Value.ForegroundColor);
                Assert.AreEqual(ConsoleColor.Green, bmp.GetPixel(x, 0).Value.Value.BackgroundColor);
            }

            // the last 4 characters should get trimmed here
            var readBack = bmp.ToConsoleString(trimMode: true);
            Assert.AreEqual("Adam".ToYellow(bg: ConsoleColor.Green), readBack);
        }

        [TestMethod]
        public void TestConvertBetweenConsoleBitmapAndConsoleStringMultiLine()
        {
            var str = "Adam".ToYellow(bg: ConsoleColor.Green) + "\n".ToConsoleString() + "Abdelhamed".ToYellow(bg: ConsoleColor.Green);
            var bmp = str.ToConsoleBitmap();
            Assert.AreEqual(10, bmp.Width);
            Assert.AreEqual(2, bmp.Height);
            Assert.AreEqual('A', bmp.GetPixel(0, 0).Value.Value.Value);
            Assert.AreEqual('d', bmp.GetPixel(1, 0).Value.Value.Value);
            Assert.AreEqual('a', bmp.GetPixel(2, 0).Value.Value.Value);
            Assert.AreEqual('m', bmp.GetPixel(3, 0).Value.Value.Value);

            Assert.AreEqual('A', bmp.GetPixel(0, 1).Value.Value.Value);
            Assert.AreEqual('b', bmp.GetPixel(1, 1).Value.Value.Value);
            Assert.AreEqual('d', bmp.GetPixel(2, 1).Value.Value.Value);
            Assert.AreEqual('e', bmp.GetPixel(3, 1).Value.Value.Value);
            Assert.AreEqual('l', bmp.GetPixel(4, 1).Value.Value.Value);
            Assert.AreEqual('h', bmp.GetPixel(5, 1).Value.Value.Value);
            Assert.AreEqual('a', bmp.GetPixel(6, 1).Value.Value.Value);
            Assert.AreEqual('m', bmp.GetPixel(7, 1).Value.Value.Value);
            Assert.AreEqual('e', bmp.GetPixel(8, 1).Value.Value.Value);
            Assert.AreEqual('d', bmp.GetPixel(9, 1).Value.Value.Value);

            var pixelsWithValueCount = 0;
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    if (bmp.GetPixel(x, y).Value.HasValue && bmp.GetPixel(x, y).Value.Value != ' ')
                    {
                        pixelsWithValueCount++;
                        Assert.AreEqual(ConsoleColor.Yellow, bmp.GetPixel(x, y).Value.Value.ForegroundColor);
                        Assert.AreEqual(ConsoleColor.Green, bmp.GetPixel(x, y).Value.Value.BackgroundColor);
                    }
                }
            }
            Assert.AreEqual("AdamAbdelhamed".Length, pixelsWithValueCount);
            var readBack = bmp.ToConsoleString(trimMode: true);
            Assert.AreEqual(str, readBack);
        }

        [TestMethod]
        public void TestConvertBetweenConsoleBitmapAndConsoleStringSingleLine()
        {
            var str = "Adam".ToYellow(bg: ConsoleColor.Green);
            var bmp = str.ToConsoleBitmap();
            Assert.AreEqual(4, bmp.Width);
            Assert.AreEqual(1, bmp.Height);
            Assert.AreEqual('A', bmp.GetPixel(0, 0).Value.Value.Value);
            Assert.AreEqual('d', bmp.GetPixel(1, 0).Value.Value.Value);
            Assert.AreEqual('a', bmp.GetPixel(2, 0).Value.Value.Value);
            Assert.AreEqual('m', bmp.GetPixel(3, 0).Value.Value.Value);

            for (var x = 0; x < bmp.Width; x++)
            {
                Assert.AreEqual(ConsoleColor.Yellow, bmp.GetPixel(x, 0).Value.Value.ForegroundColor);
                Assert.AreEqual(ConsoleColor.Green, bmp.GetPixel(x, 0).Value.Value.BackgroundColor);
            }

            var readBack = bmp.ToConsoleString();
            Assert.AreEqual(str, readBack);

            var andBackAgain = readBack.ToConsoleBitmap();
            Assert.AreEqual(bmp, andBackAgain);
        }

        [TestMethod]
        public void TestConsoleStringEncodingBasic()
        {
            var inputString = "A".ToRed() + "BC".ToWhite(bg: ConsoleColor.Red) + "DE".ToBlue() + "FG".ToConsoleString();

            var serialized = inputString.Serialize();
            var readBack = ConsoleString.Parse(serialized);
            Assert.AreEqual(inputString, readBack);

            var serializedImplicitDefaults = inputString.Serialize(true);
            var readBackImplicitDefaults = ConsoleString.Parse(serialized);
            Assert.AreEqual(inputString, readBackImplicitDefaults);
        }

        [TestMethod]
        public void TestConsoleStringEncodingEdgeCases()
        {
            var parsed = ConsoleString.Parse("[B=Blue][Red]Adam");
            Assert.AreEqual(new ConsoleString("Adam", ConsoleColor.Red, ConsoleColor.Blue), parsed);
            parsed = ConsoleString.Parse("[  B   =   Blue ][ Red ]Adam");
            Assert.AreEqual(new ConsoleString("Adam", ConsoleColor.Red, ConsoleColor.Blue), parsed);

            parsed = ConsoleString.Parse(@"\[Adam\]");
            Assert.AreEqual(new ConsoleString("[Adam]"), parsed);
            parsed = ConsoleString.Parse(@"\[Adam]");
            Assert.AreEqual(new ConsoleString("[Adam]"), parsed);

            var original = "[Adam]";
            var s = original.ToConsoleString().Serialize();
            var back = ConsoleString.Parse(s);
            Assert.AreEqual(original, back.StringValue);
        }

        [TestMethod]
        public void TestConsoleStringImplicitDefaultMode()
        {
            var defaultFg = new ConsoleCharacter(' ').ForegroundColor;
            var defaultBg = new ConsoleCharacter(' ').BackgroundColor;
            Assert.AreEqual($"[{defaultFg}][B={defaultBg}]Adam", new ConsoleString("Adam").Serialize());
            Assert.AreEqual("Adam", new ConsoleString("Adam").Serialize(true));
        }


        [TestMethod]
        public void TestConsoleStringSplit()
        {
            // It will split on the space here because the styles match
            var split = "Adam Abdelhamed".ToConsoleString().Split(" ".ToConsoleString());
            Assert.AreEqual(2, split.Count);
            Assert.AreEqual("Adam", split[0].StringValue);
            Assert.AreEqual("Abdelhamed", split[1].StringValue);

            // Make sure those extra spaces don't make it into the split
            split = "Adam                   Abdelhamed".ToConsoleString().Split(" ".ToConsoleString());
            Assert.AreEqual(2, split.Count);
            Assert.AreEqual("Adam", split[0].StringValue);
            Assert.AreEqual("Abdelhamed", split[1].StringValue);

            // It won't split on the space here because the style is different
            split = ("Adam".ToConsoleString() + " ".ToYellow() + "Abdelhamed".ToConsoleString()).Split(" ".ToConsoleString());
            Assert.AreEqual(1, split.Count);
            Assert.AreEqual("Adam Abdelhamed", split[0].StringValue);
        }

        [TestMethod]
        public void TestConsoleStringToDiv()
        {
            var str = "Adam".ToRed() + " ".ToConsoleString() + "Abdelhamed".ToGreen();
            var toDiv = str.ToHtmlDiv(indent: false);
            Assert.AreEqual("<div class='powerargs-console-string' style='font-family:Consolas;background-color:black'><span style='color:red;background-color:black;'>Adam</span><span style='color:grey;background-color:black;'> </span><span style='color:green;background-color:black;'>Abdelhamed</span></div>", toDiv);
        }

        private static void ValidateStringCharacteristics(string expected, ConsoleString actual)
        {
            Assert.AreEqual(expected, string.Join("", actual.Select(c => c.Value)));
            Assert.AreEqual(0, actual.CompareTo(expected));
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
