using System;

namespace PowerArgs
{
    /// <summary>
    /// An attribute that, when placed on a property or action method, makes sure that property/action does not appear
    /// in the output created by the ArgUsage class (the class that auto generates usage documentation).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
    public class OmitFromUsageDocs : Attribute, IArgumentOrActionMetadata
    {

    }
}
