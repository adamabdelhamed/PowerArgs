using System;
using System.IO;
using System.Threading;

namespace PowerArgs
{
    /// <summary>
    /// A class that PowerArgs uses internally for diagnostics
    /// </summary>
    public static class PowerLogger
    {
        private static string _logFile = null;

        /// <summary>
        /// The log file to write to or "Console" if logs should be written to the console
        /// </summary>
        public static string LogFile
        {
            get
            {
                return _logFile;
            }
            set
            {

                try
                {
                    var dir = Path.GetDirectoryName(value);
                    if(Directory.Exists(dir) == false)
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                catch (Exception) { }

                _logFile = value;
            }
        }

        private static object logLock = new object();

        /// <summary>
        /// Logs a line of text
        /// </summary>
        /// <param name="s">the text to log</param>
        /// <param name="retryCount">don't use</param>
        public static void LogLine(string s, int retryCount = 0)
        {
            if (retryCount == 3) return;

            if(LogFile == null)return;

            if (LogFile == "Console")
            {
                Console.WriteLine(s);
            }
            else
            {
                lock (logLock)
                {
                    try
                    {
                        File.AppendAllText(LogFile, s + Environment.NewLine);
                    }
                    catch(Exception ex)
                    {
                        Thread.Sleep(5);
                        LogLine(s, retryCount + 1);
                    }
                }
            }
        }
    }
}