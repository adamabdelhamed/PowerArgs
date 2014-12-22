using Hammer.Util;
using PowerArgs;
using System;
using System.IO;
using System.Linq;

namespace DoxygenPublisher
{
    [ArgActions]
    public class PublishDoxygenDocsCommand
    {
        [ArgActionMethod, ArgDescription("Publishes doxygen docs for PowerArgs to the web")]
        public static void PublishDoxygenDocs(PublishDoxygenDocsArgs args)
        {
            var doxygenResult = RunDoxygen(args.DoxyFile);
            Helpers.ClearContainer(args.Container, doxygenResult.PowerArgsVersion);
            var uploadResult = Helpers.UploadFiles(args.Container, doxygenResult.OutputDirectory, doxygenResult.PowerArgsVersion + "/");

            var indexFile = (from r in uploadResult.Results where Path.GetFileName(r.LocalFile).Equals("index.html", StringComparison.OrdinalIgnoreCase) select r).SingleOrDefault();
            CodeBouncer.ExpectNotNull(indexFile, "There was no index.html file in the documentation output");

            ConsoleString.WriteLine("Docs published: " + indexFile.RemoteFile.Uri.ToString(), ConsoleColor.Cyan);
        }

        private static DoxygenResult RunDoxygen(string doxyFileTemplate)
        {
            var repoRoot = Path.GetDirectoryName(doxyFileTemplate);
            var powerArgsProjectRoot = Path.Combine(repoRoot, "PowerArgs");
            var powerArgsVersion = Helpers.FindPowerArgsVersion(powerArgsProjectRoot);
            var outputDirectory = Path.Combine(repoRoot, "DoxygenOutput", powerArgsVersion);
            var tempDoxyFilePath = Path.Combine(Path.GetTempPath(), "Doxyfile");

            RenderEffectiveDoxyFile(doxyFileTemplate, outputDirectory, powerArgsVersion, powerArgsProjectRoot, tempDoxyFilePath);
            Helpers.EnsureEmptyDirectory(outputDirectory);
            Helpers.Run("doxygen", "\"" + tempDoxyFilePath + "\"");
            Helpers.DeleteTempFileBestEffort(tempDoxyFilePath);

            return new DoxygenResult()
            {
                OutputDirectory = outputDirectory,
                PowerArgsVersion = powerArgsVersion,
            };
        }

        private static void RenderEffectiveDoxyFile(string templateFilePath, string outputDirectory, string version, string sourceDirectory, string tempFilePath)
        {
            var renderer = new DocumentRenderer();
            var template = File.ReadAllText(templateFilePath);
            var effectiveDoxyFileContents = renderer.Render(template, new
            {
                OutputDirectory = outputDirectory.Replace("\\", "/"),
                Version = version,
                SourceDirectory = sourceDirectory.Replace("\\", "/"),
            }).ToString();

            File.WriteAllText(tempFilePath, effectiveDoxyFileContents);
        }
    }

    public class DoxygenResult
    {
        public string OutputDirectory { get; set; }
        public string PowerArgsVersion { get; set; }
    }
}

