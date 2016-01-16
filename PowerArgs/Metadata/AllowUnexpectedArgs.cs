using System;

namespace PowerArgs
{
    /// <summary>
    /// Put this attribute on the class that defines your arguments to specify that PowerArgs
    /// should allow extra command line values that don't match any explicitly defined arguments.
    /// Note that this means that PowerArgs will not be able to tell the difference between an extra
    /// argument and a misspelled argument.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AllowUnexpectedArgs : Attribute, IGlobalArgMetadata
    {

    }
    
}
