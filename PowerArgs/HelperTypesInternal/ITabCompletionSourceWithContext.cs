
using System;
namespace PowerArgs
{
    [Obsolete("Use ISmartTabCompletionSource.  It gives you much better context you can use for tab completion.")]
    internal interface ITabCompletionSourceWithContext : ITabCompletionSource
    {
        bool TryComplete(bool shift, string context, string soFar, out string completion);
    }
}
