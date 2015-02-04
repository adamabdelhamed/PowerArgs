
namespace PowerArgs
{
    /// <summary>
    /// An interface that defines an object that implements ToConsoleString
    /// </summary>
    public interface ICanBeAConsoleString
    {
        /// <summary>
        /// Formats this object as a ConsoleString
        /// </summary>
        /// <returns>a ConsoleString</returns>
        ConsoleString ToConsoleString();
    }
}
