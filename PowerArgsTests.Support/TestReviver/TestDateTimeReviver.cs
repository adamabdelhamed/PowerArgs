using System;
using System.Globalization;
using PowerArgs;

namespace PowerArgsTests.Support.TestReviver
{
    public class TestDateTimeReviver
    {
        [ArgReviver]
        public static DateTime ReviveDate(string key, string val)
        {
            DateTime date;
            if (DateTime.TryParse(val, out date))
            {
                return date;
            }

            string[] format = { "yyyy/MM/dd", "MM/dd/yyyy", "yyyyMMdd" };
            if (DateTime.TryParseExact(val, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date;
            }

            throw new ArgException(string.Format("Value: {0} can not be parsed as DateTime", val), new FormatException(string.Format("Value: {0} can not be parsed as DateTime", val)));
        }
    }
}
