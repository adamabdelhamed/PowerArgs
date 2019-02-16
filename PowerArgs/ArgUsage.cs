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

    public static class UsageTemplates
    {

        public const string ConsoleTemplate =
@"{{if HasDescription}}

{{ Description !}}


!{{if}}
{{ifnot HasSpecifiedAction}}
Usage - {{UsageSummary Cyan!}}
!{{ifnot}}
{{if HasGlobalUsageArguments}}

{{table UsageArguments Syntax>GlobalOption Description+ !}}
!{{if}}
{{if HasActions}}
{{if HasSpecifiedAction}}

{{SpecifiedAction.DefaultAlias!}} - {{SpecifiedAction.Description!}}

Usage - {{ExeName Cyan!}} {{SpecifiedAction.UsageSummary Cyan!}}

{{if SpecifiedAction.HasUsageArguments }}
{{SpecifiedAction.DefaultAlias!}} Options
{{table SpecifiedAction.UsageArguments Syntax>Option Description+ !}}
!{{if}}
!{{if}}
{{ifnot HasSpecifiedAction}}

{{if HasUsageActions}}
Actions
{{each action in UsageActions}}

  {{action.UsageSummary Cyan!}} - {{action.Description!}}

{{if action.HasUsageArguments }}
    {{table action.UsageArguments Syntax>Option Description+ !}}
!{{if}}
{{if action.HasExamples }}

    {{action.DefaultAlias!}} Examples{{each example in action.Examples}}

    {{if example.HasTitle}}{{example.Title Cyan!}} - !{{if}}{{example.Description!}}
    {{example.Example Green!}}!{{each}}

!{{if}}
!{{each}}
!{{if}}
!{{ifnot}}
!{{if}}
{{if HasExamples }}

Examples{{each example in Examples}}

    {{if example.HasTitle}}{{example.Title Cyan!}} - !{{if}}{{example.Description!}}
    {{example.Example Green!}}
!{{each}}

!{{if}}
";

        public const string BrowserTemplate =
@"<!DOCTYPE html>
<html xmlns='http://www.w3.org/1999/xhtml'>
<head>
    <title>{{ExeName!}} documentation</title>
</head>
<body>
    <h1 class='program-specific-content'>{{ExeName!}}</h1>
    <p class='program-specific-content'>{{Description!}}</p>

    <h2>Usage</h2>
<pre class='code-sample'>{{UsageSummaryHTMLEncoded!}}</pre>

    {{if HasGlobalUsageArguments}}
    {{if HasActions}}<h2>Global options</h2>!{{if}}
    {{ifnot HasActions}}<h2>Options</h2>!{{ifnot}}

    <table>
        <tr>
            <td class='option-col table-header'>OPTION</td>
            <td class='desc-col table-header'>DESCRIPTION</td>
        </tr>
        {{each argument in Arguments}}
        {{if argument.IncludeInUsage}}
        <tr>
            <td class='option-col program-specific-content'>-{{argument.DefaultAlias!}}</td>
            <td class='desc-col program-specific-content'>{{argument.Description!}}{{if argument.HasDefaultValue}}<span class='defaultvalue'> Default<span /><span class='defaultvalue'>=<span /><span class='defaultvalue'>{{argument.DefaultValue!}}<span />!{{if}}</td>
        </tr>
        {{if argument.IsEnum}}
        {{each enumVal in argument.EnumValuesAndDescriptions}}
        <tr>
            <td class='option-col program-specific-content'></td>
            <td class='defaultValue'> {{enumVal!}}</td>
        </tr>
        !{{each}}
        !{{if}}
        !{{if}}
        !{{each}}
    </table>
    !{{if}}

    {{if HasActions}}
    <h2>Actions</h2>
    <ul>
        {{each action in UsageActions}}
        <li>
            <div>
                <span class='program-specific-content'>{{action.DefaultAlias!}}</span>
                <span> - </span>
                <span class='program-specific-content'>{{action.Description!}}</span>
                <button id='action{{action-index!}}ExpandButton' onclick='toggleAction({{action-index!}})' class='expander-button'>{{ifnot action.IsSpecifiedAction}}show details!{{ifnot}}{{if action.IsSpecifiedAction}}hide details!{{if}}</button>
                <div id='action{{action-index!}}details' class='expandable {{ifnot action.IsSpecifiedAction}}hidden!{{ifnot}}'>
                    <h4>{{action.DefaultAlias!}} Usage</h4>
                    <pre class='code-sample'>{{ExeName!}} {{action.UsageSummaryHTMLEncoded!}}</pre>
                    <h4>Action options</h4>
                    <table>
                        <tr>
                            <td>OPTION</td>
                            <td>DESCRIPTION</td>
                        </tr>
                        {{each actionArgument in action.UsageArguments}}
                        <tr>
                            <td class='option-col program-specific-content'>{{actionArgument.DefaultAlias!}}</td>
                            <td class='desc-col program-specific-content'>-{{actionArgument.Description!}}{{if actionArgument.HasDefaultValue}}<span class='defaultvalue'> Default<span /><span class='defaultvalue'>=<span /><span class='defaultvalue'>{{actionArgument.DefaultValue!}}<span />!{{if}}</td>
                        </tr>
                        {{if actionArgument.IsEnum}}
                        {{each enumVal in actionArgument.EnumValuesAndDescriptions}}
                        <tr>
                            <td class='option-col program-specific-content'></td>
                            <td class='defaultValue'> {{enumVal!}}</td>
                        </tr>
                        !{{each}}
                        !{{if}}
                        !{{each}}
                    </table>
                    {{if action.HasExamples }}
                    <h4>Examples</h4>
                    {{each example in action.Examples}}
                    <h3>{{example.Title!}}</h3>
                    <p>{{example.Description!}}</p>
                    <pre class='code-sample'>{{example.Example!}}</pre>
                    !{{each}}
                    !{{if}}
                </div>
            </div>
        </li>
        !{{each}}
    </ul>
    !{{if}}

    {{if HasExamples }}
    <h2>Examples</h2>
    {{each example in Examples}}
    <h3>{{example.Title!}}</h3>
    <p>{{example.Description!}}</p>
    <pre class='code-sample'>{{example.Example!}}</pre>
    !{{each}}
    !{{if}}
</body>

<script>
    function toggleAction(index)
    {
        var toggleElement = document.getElementById('action' + index + 'details');
        var toggleButton = document.getElementById('action' + index + 'ExpandButton');

        if (toggleElement.classList.contains('hidden'))
        {
            toggleElement.classList.remove('hidden');
            toggleButton.innerHTML = 'hide details';
        }
        else
        {
            toggleElement.classList.add('hidden');
            toggleButton.innerHTML = 'show details';
        }
    }
</script>
<style>
    html
    {
        font-family: Verdana, Geneva, sans-serif;
        padding: 0;
        margin: 0;
        color: #333;
    }

    body
    {
        margin: 14px;
    }

    h1
    {
        font-size: 24px;
    }

    ul
    {
        padding-left: 0;
        margin-left: 7px;
    }

    li
    {
        list-style-type: none;
        padding-left: 0;
        margin-left: 0;
    }

    h2
    {
        margin-top: 24px;
        margin-bottom: 24px;
        color: #777;
        font-size: 20px;
    }

    h3
    {
        margin-top: 21px;
        margin-bottom: 21px;
        color: #777;
        font-size: 16px;
    }

    h4
    {
        margin-top: 7px;
        margin-bottom: 7px;
        color: #777;
        font-size: 16px;
        font-weight: light;
    }

    h5
    {
        color: #777;
        font-size: 14px;
    }

    button
    {
        text-transform: uppercase;
        border: 0;
        background-color: transparent;
        color: #00C3FF;
        cursor: pointer;
        padding: 0;
        display: inline;
    }

    .hidden
    {
        display: none;
    }

    .expandable
    {
        background-color: #eee;
        padding: 14px;
    }

    .expander-button
    {
        margin-left: 7px;
    }

    .code-sample
    {
        font-family: 'Courier New', Courier, monospace;
        font-weight: bold;
        background-color: #A3D8FF;
        color: #333;
        padding-top: 15px;
        padding-bottom: 15px;
        padding-left: 5px;
        padding-right: 0px;
        width: 100%;
    }

    .program-specific-content
    {
        color: #005DA0;
    }

    .table-header
    {
        font-weight: bold;
    }

    .option-col
    {
        padding-top: 14px;
        padding-left: 14px;
        padding-right: 14px;
    }


    .desc-col
    {
        padding-top: 14px;
        max-width: 600px;
        vertical-align: text-top;
    }

    .defaultvalue
    {
        font-weight: bold;
    }
</style>
</html>";


    }
}
