using System.Runtime.InteropServices;

namespace PowerArgs
{
    /// <summary>
    /// 
    /// https://mariusschulz.com/blog/detecting-the-operating-system-in-net-core
    /// </summary>
    public static class OperatingSystem
    {
        /// <summary>
        /// Indicates if assembly is running on Windows OS
        /// </summary>
        /// <returns>True if running on Windows OS, false otherwise</returns>
        public static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}