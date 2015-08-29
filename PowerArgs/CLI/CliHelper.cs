using System;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that provides a framework for building interactive command line interfaces.
    /// </summary>
    public class CliHelper
    {
        /// <summary>
        /// Gets or sets the reader to use when reading input from the console
        /// </summary>
        public RichTextCommandLineReader Reader { get; set; }

        /// <summary>
        /// Creates a new CLI object.
        /// </summary>
        public CliHelper()
        {
            Reader = new RichTextCommandLineReader() { Console = ConsoleProvider.Current };
        }

        /// <summary>
        /// Prompts the user to select a value from a set of options.
        /// </summary>
        /// <param name="message">the prompt message</param>
        /// <param name="options">the options to choose from</param>
        /// <returns>The selected value</returns>
        public string Prompt(string message, params string[] options)
        {
            return Prompt(new ConsoleString(message, ConsoleColor.Yellow), options);
        }

        /// <summary>
        /// Prompts the user to select a value from a set of options.
        /// </summary>
        /// <param name="message">the prompt message</param>
        /// <param name="options">the options to choose from</param>
        /// <returns>The selected value</returns>
        public string Prompt(ConsoleString message, params string[] options)
        {
            var optionsString = new ConsoleString("(" + string.Join("/", options) + ")", ConsoleColor.Cyan);
            var prompt = message + new ConsoleString(" ") + optionsString + ": ";
            prompt.Write();

            var option = Reader.ReadLine().ToString();

            if (options.Contains(option, StringComparer.InvariantCultureIgnoreCase) == false)
            {
                Console.WriteLine("Unrecognized option: " + option);
                return Prompt(message, options);
            }
            else
            {
                return option;
            }
        }

        /// <summary>
        /// Asks the user if they are sure about performing some operation and returns true if they indicate yes and
        /// false if they indicate no.
        /// </summary>
        /// <param name="about">The message to display.  'Are you sure?' will be apended.</param>
        /// <returns>true if they indicate yes and false if they indicate no.</returns>
        public bool IsUserSure(string about)
        {
            return IsUserSure(new ConsoleString(about, ConsoleColor.Yellow));
        }

        /// <summary>
        /// Asks the user if they are sure about performing some operation and returns true if they indicate yes and
        /// false if they indicate no.
        /// </summary>
        /// <param name="about">The message to display.  'Are you sure?' will be apended.</param>
        /// <returns>true if they indicate yes and false if they indicate no.</returns>
        public bool IsUserSure(ConsoleString about)
        {
            if (about.EndsWith("."))
            {
                about = about.Substring(0, about.Length - 1);
            }

            var response = Prompt(about + ".  Are you sure?", "y", "n");
            if (response.Equals("y", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Prompts the user for a line of input with the given message
        /// </summary>
        /// <param name="message">the prompt message</param>
        /// <returns>the input that the user entered</returns>
        public string PromptForLine(string message)
        {
            if(message.EndsWith(": ") == false)
            {
                message += ": ";
            }
            ConsoleString.Write(message, ConsoleColor.Yellow);
            var input = Reader.ReadLine().ToString();
            return input;
        }
    }
}
