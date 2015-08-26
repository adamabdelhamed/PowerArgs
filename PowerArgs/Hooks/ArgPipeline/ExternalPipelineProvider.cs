using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PowerArgs.Preview
{
    /// <summary>
    /// If you are thinking about using this attribute then you are doing something very advanced.  
    /// By default, PowerArgs' pipeline feature lets you pipe objects between commands running in the same process.  There is, however, an extensibility point that
    /// lets you pipe objects between processes.  PowerArgs provides an implementation using a local HTTP listener that you
    /// can use by adding the nuget package PowerArgs.HttpExternalPipelineProvider.  It is not included by default because it brings in a dependency on Json.NET and 
    /// I didn't want all PowerArgs users to have to take that dependency.  As long as that DLL is living side by side with PowerArgs.dll then you'll
    /// get the inter process piping support for free.  This attribute should only be used if you want to provide a different implementation.  You should only need to do that
    /// if the Http solution does not work for you for some reason.  If that's the case, please submit an issue to GitHub we can discuss your requirements.  Ideally we would
    /// try our best to make the HTTP solution work for you.  If we can't then I can document this extensibility point.  Keep in mind that if you do build your own implementation
    /// then only programs that are built with your implementation will be able to pipe objects between each other.
    /// </summary>
    public class ExternalOutputPipelineStageProviderAttribute : Attribute { }

    /// <summary>
    /// If you are thinking about using this attribute then you are doing something very advanced.  
    /// By default, PowerArgs' pipeline feature lets you pipe objects between commands running in the same process.  There is, however, an extensibility point that
    /// lets you pipe objects between processes.  PowerArgs provides an implementation using a local HTTP listener that you
    /// can use by adding the nuget package PowerArgs.HttpExternalPipelineProvider.  It is not included by default because it brings in a dependency on Json.NET and 
    /// I didn't want all PowerArgs users to have to take that dependency.  As long as that DLL is living side by side with PowerArgs.dll then you'll
    /// get the inter process piping support for free.  This attribute should only be used if you want to provide a different implementation.  You should only need to do that
    /// if the Http solution does not work for you for some reason.  If that's the case, please submit an issue to GitHub we can discuss your requirements.  Ideally we would
    /// try our best to make the HTTP solution work for you.  If we can't then I can document this extensibility point.  Keep in mind that if you do build your own implementation
    /// then only programs that are built with your implementation will be able to pipe objects between each other.
    /// </summary>
    public class ExternalInputPipelineStageProviderAttribute : Attribute { }

    internal static class ExternalPipelineProvider
    {
        internal static object searchAssemblyCacheLock = new object();
        internal static List<Assembly> searchAssemblyCache = null;

        internal static bool TryLoadOutputStage(string[] commandLine, out PipelineStage result)
        {
            return TryLoadAddInObject<ExternalOutputPipelineStageProviderAttribute, PipelineStage>(out result, new object[]{ commandLine});
        }

        internal static bool TryLoadInputStage(CommandLineArgumentsDefinition definition, string[] commandLine, out ExternalPipelineInputStage result)
        {
            return TryLoadAddInObject<ExternalInputPipelineStageProviderAttribute, ExternalPipelineInputStage>(out result, definition, commandLine);  
        }

        private static bool TryLoadAddInObject<TAttr, TBaseReq>(out TBaseReq result, params object[] constructorParams)
        {
            Type targetType = null;
           
            var powerArgsDir = Path.GetDirectoryName(typeof(Args).Assembly.Location);
                
            foreach(Assembly toInspect in GetSearchAssemblyCache())
            {
                try
                {
                    var match = (from t in toInspect.GetTypes() where t.HasAttr<TAttr>() && (t.GetInterfaces().Contains(typeof(TBaseReq)) || t.IsSubclassOf(typeof(TBaseReq))) select t).FirstOrDefault();
                    if (match != null)
                    {
                        targetType = match;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    PowerLogger.LogLine("Exception trying to reflect over an assembly to get type info: "+ex.ToString());
                }
            }

            if(targetType == null)
            {
                PowerLogger.LogLine("Could not find an object with a base of "+typeof(TBaseReq).FullName+" that had attribute "+typeof(TAttr).FullName);
                result = default(TBaseReq);
                return false;
            }

            try
            {
                result = (TBaseReq)Activator.CreateInstance(targetType, constructorParams);
                return true;
            }
            catch(TargetInvocationException ex)
            {
                if (ex.InnerException == null)
                {
                    throw;
                }
                else if (ex.InnerException is ArgException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }

                result = default(TBaseReq);
                return false;
            }
            catch(Exception ex)
            {
                PowerLogger.LogLine("Could not initialize target type "+targetType.FullName+"\n\n"+ex.ToString());
                result = default(TBaseReq);
                return false;
            }
        }

        private static List<Assembly> GetSearchAssemblyCache()
        {
            lock (searchAssemblyCacheLock)
            {
                if (searchAssemblyCache == null)
                {
                    searchAssemblyCache = new List<Assembly>();
                    var powerArgsDir = Path.GetDirectoryName(typeof(Args).Assembly.Location);
                    foreach (var file in Directory.GetFiles(powerArgsDir).Where(f => f.ToLower().EndsWith(".dll")))
                    {
                        Assembly toInspect;
                        if (TryLoadAssemblyFromFile(file, out toInspect))
                        {
                            searchAssemblyCache.Add(toInspect);
                        }
                    }
                }
            }

            return searchAssemblyCache;
        }

        private static bool TryLoadAssemblyFromFile(string file, out Assembly result)
        {
            try
            {
                result = Assembly.LoadFile(file);
                return true;
            }
            catch(Exception ex)
            {
                result = null;
                return false;
            }
        }
    }
}
