using System;
using PowerArgs;

namespace PowerArgsTests.Support.TestReviver
{
    public class TestDateTimeReviver
    {
        [ArgReviver]
        public static DateTime ReviveDate(string key, string val)
        {
            return DateTime.Today;
        }
    }
}
