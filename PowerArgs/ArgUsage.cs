using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace PowerArgs
{
    /// <summary>
    /// A helper class that generates usage documentation for your command line arguments given a custom argument
    /// scaffolding type.
    /// </summary>
    public static class ArgUsage
    {
        /// <summary>
        /// Generates a usage document given a template
        /// </summary>
        /// <typeparam name="T">The command line argument definition scaffold type</typeparam>
        /// <param name="template">The template to use or null to use the default template that's built into PowerArgs</param>
        /// <returns>The usage document</returns>
        public static ConsoleString GenerateUsageFromTemplate<T>(string template = null)
        {
            return GenerateUsageFromTemplate(new CommandLineArgumentsDefinition(typeof(T)), template);
        }

        /// <summary>
        /// Generates a usage document given a template
        /// </summary>
        /// <param name="t">The command line argument definition scaffold type</param>
        /// <param name="template">The template to use or null to use the default template that's built into PowerArgs</param>
        /// <returns>The usage document</returns>
        public static ConsoleString GenerateUsageFromTemplate(Type t, string template = null)
        {
            return GenerateUsageFromTemplate(new CommandLineArgumentsDefinition(t), template);
        }

        /// <summary>
        /// Generates a usage document given a template
        /// </summary>
        /// <param name="def">The object that describes your program</param>
        /// <param name="template">The template to use or null to use the default template that's built into PowerArgs</param>
        /// <param name="templateSourceLocation">The source of the template, usually a file name</param>
        /// <returns>The usage document</returns>
        public static ConsoleString GenerateUsageFromTemplate(CommandLineArgumentsDefinition def, string template = null, string templateSourceLocation = null)
        {
            bool needsContextCleanup = false;
            if(ArgHook.HookContext.Current == null)
            {
                needsContextCleanup = true;
                ArgHook.HookContext.Current = new ArgHook.HookContext();
                ArgHook.HookContext.Current.Definition = def;
            }

            try
            {
                if (ArgHook.HookContext.Current.Definition == def)
                {
                    ArgHook.HookContext.Current.RunBeforePrepareUsage();
                }

                if (template == null)
                {
                    template = UsageTemplates.ConsoleTemplate;
                    templateSourceLocation = "Default console usage template";
                }
                var document = new DocumentRenderer().Render(template, def, templateSourceLocation);
                return document;
            }
            finally
            {
                if(needsContextCleanup)
                {
                    ArgHook.HookContext.Current = null;
                }
            }
        }

        /// <summary>
        /// Generates web browser friendly usage documentation for your program and opens it using the local machine's default browser.
        /// </summary>
        /// <typeparam name="T">The command line argument definition scaffold type</typeparam>
        /// <param name="template">The template to use or null to use the default browser friendly template that's built into PowerArgs</param>
        /// <param name="outputFileName">Where to save the output (the browser will open the file from here)</param>
        /// <param name="deleteFileAfterBrowse">True if the file should be deleted after browsing</param>
        /// <param name="waitForBrowserExit">True if you'd like this method to block until the browser is closed.  This only works for browsers that start a new process when opened with a document.</param>
        /// <returns>The usage document as a string</returns>
        public static string ShowUsageInBrowser<T>(string template = null, string outputFileName = null, bool deleteFileAfterBrowse = true, bool waitForBrowserExit = true)
        {
            return ShowUsageInBrowser(new CommandLineArgumentsDefinition(typeof(T)), template, outputFileName, deleteFileAfterBrowse, waitForBrowserExit);
        }

        /// <summary>
        /// Generates web browser friendly usage documentation for your program and opens it using the local machine's default browser.
        /// </summary>
        /// <param name="def">The object that describes your program</param>
        /// <param name="template">The template to use or null to use the default browser friendly template that's built into PowerArgs</param>
        /// <param name="outputFileName">Where to save the output (the browser will open the file from here)</param>
        /// <param name="deleteFileAfterBrowse">True if the file should be deleted after browsing</param>
        /// <param name="waitForBrowserExit">True if you'd like this method to block until the browser is closed.  This only works for browsers that start a new process when opened with a document.</param>
        /// <returns>The usage document as a string</returns>
        public static string ShowUsageInBrowser(CommandLineArgumentsDefinition def, string template = null, string outputFileName = null, bool deleteFileAfterBrowse = true, bool waitForBrowserExit = true)
        {
            var usage = ArgUsage.GenerateUsageFromTemplate(def, template ?? UsageTemplates.BrowserTemplate);
            outputFileName = outputFileName ?? Path.GetTempFileName().ToLower().Replace(".tmp", ".html");
            Process proc = null;
            try
            {
                File.WriteAllText(outputFileName, usage.ToString());
                proc = Process.Start(outputFileName);
                if (proc != null && waitForBrowserExit)
                {
                    proc.WaitForExit();
                }
            }
            finally
            {
                if (deleteFileAfterBrowse)
                {
                    if (File.Exists(outputFileName))
                    {
                        if(waitForBrowserExit == false || proc == null)
                        {
                            Thread.Sleep(3000); // Gives the browser a few seconds to read the file before deleting it.
                        }

                        File.Delete(outputFileName);
                    }
                }
            }
            return usage.ToString();
        }
     }
}
