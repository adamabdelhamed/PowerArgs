using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    internal static class UsageTemplateProvider
    {
        public static ConsoleString GetUsage(Type usageTemplateProviderType, CommandLineArgumentsDefinition definition)
        {
            if (usageTemplateProviderType.GetInterfaces().Contains(typeof(IUsageTemplateProvider)) == false)
            {
                throw new InvalidArgDefinitionException("The UsageTemplateProviderType "+usageTemplateProviderType.FullName+" does not implement " + typeof(IUsageTemplateProvider).Name);
            }

            var provider = Activator.CreateInstance(usageTemplateProviderType) as IUsageTemplateProvider;
            string template = provider.GetTemplate();
            var usage = ArgUsage.GenerateUsageFromTemplate(definition, template);
            return usage;
        }
    }

    /// <summary>
    /// An interface that defines how usage templates should be retrieved
    /// </summary>
    public interface IUsageTemplateProvider
    {
        /// <summary>
        /// Gets the usage template to render
        /// </summary>
        /// <returns>usage template to render</returns>
        string GetTemplate();
    }

    /// <summary>
    /// A usage template provider that returns the default console usage template
    /// </summary>
    public class DefaultConsoleUsageTemplateProvider : IUsageTemplateProvider
    {
        /// <summary>
        /// gets the default console usage template
        /// </summary>
        /// <returns>the default console usage template</returns>
        public string GetTemplate()
        {
            return Resources.DefaultConsoleUsageTemplate;
        }
    }

    /// <summary>
    /// A usage template provider that returns the default browser usage template
    /// </summary>
    public class DefaultBrowserUsageTemplateProvider : IUsageTemplateProvider
    {
        /// <summary>
        /// gets the default browser usage template
        /// </summary>
        /// <returns>the default browser usage template</returns>
        public string GetTemplate()
        {
            return Resources.DefaultBrowserUsageTemplate;
        }
    }
}
