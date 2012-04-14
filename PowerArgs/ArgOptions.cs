using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class ArgSettings
    {
        internal const string ActionArgConventionSuffix = "Args";
        internal const string ActionPropertyConventionName = "Action";
    }

    public class ArgOptions
    {
        public static ArgOptions DefaultOptions
        {
            get
            {
                return new ArgOptions()
                {
                    IgnoreCaseForPropertyNames = true,
                    Style = ArgStyle.PowerShell
                };
            }
        }

        public ArgStyle Style { get; set; }
        public bool IgnoreCaseForPropertyNames { get; set; }
    }
}
