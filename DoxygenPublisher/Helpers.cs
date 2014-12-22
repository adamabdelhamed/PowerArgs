using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DoxygenPublisher
{
    public class UploadFilesResult
    {
        public List<UploadFileResult> Results { get; private set; }
        public UploadFilesResult()
        {
            Results = new List<UploadFileResult>();
        }

        public void Merge(UploadFilesResult other)
        {
            this.Results.AddRange(other.Results);
        }
    }

    public class UploadFileResult
    {
        public string LocalFile { get; set; }
        public CloudBlockBlob RemoteFile { get; set; }
    }

    public static class Helpers
    {
        public static string FindPowerArgsVersion(string projectRootDirectory)
        {
            var assemblyInfoFilePath = Path.Combine(projectRootDirectory, "Properties", "AssemblyInfo.cs");
            var assemblyInfoContents = File.ReadAllText(assemblyInfoFilePath);
            var match = Regex.Match(assemblyInfoContents, Helpers.MakeVersionRegex());
            if (match.Success == false)
            {
                throw new FormatException("Unable to find the version in file " + assemblyInfoFilePath);
            }

            var major = int.Parse(match.Groups["MajorVersion"].Value);
            var minor = int.Parse(match.Groups["MinorVersion"].Value);
            var rev = int.Parse(match.Groups["Revision"].Value);
            var build = int.Parse(match.Groups["BuildNumber"].Value);
            var ret = string.Format("{0}.{1}.{2}.{3}", major, minor, rev, build);
            return ret;
        }

        public static int Run(string exe, string args, bool inline = false, bool throwOnErrorStatus = true)
        {
            Console.WriteLine("running " + exe + " " + args);

            ProcessStartInfo ret = new ProcessStartInfo(exe, args);

            if (inline)
            {
                ret.CreateNoWindow = true;
                ret.UseShellExecute = false;
                ret.RedirectStandardError = true;
                ret.RedirectStandardInput = true;
                ret.RedirectStandardOutput = true;
            }

            Process process;

            try
            {
                process = Process.Start(ret);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not start " + exe + ".  Is it installed?", ex);
            }

            process.WaitForExit();

            if (inline)
            {
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                Console.WriteLine(process.StandardError.ReadToEnd());
            }

            if (throwOnErrorStatus && process.ExitCode != 0)
            {
                throw new IOException(exe + " " + args + " exited with code " + process.ExitCode);
            }

            return process.ExitCode;
        }

        public static void DeleteTempFileBestEffort(string tempFilePath)
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    Console.WriteLine("Deleted temp file: " + tempFilePath);
                }
            }
            catch (Exception) { }
        }

        public static void EnsureEmptyDirectory(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory) == false)
            {
                Directory.CreateDirectory(outputDirectory);
            }
            else
            {
                Directory.Delete(outputDirectory, true);
                Directory.CreateDirectory(outputDirectory);
            }
        }

        public static string MakeVersionRegex()
        {
            string ret = "";

            ret += "Version";

            ret += @"\(";

            ret += '"';

            ret += Group(@"\d*", "MajorVersion");
            ret += @"\.";

            ret += Group(@"\d*", "MinorVersion");
            ret += @"\.";

            ret += Group(@"\d*", "Revision");
            ret += @"\.";

            ret += Group(@"\d*", "BuildNumber");

            ret += '"';

            ret += @"\)";

            return ret;
        }

        public static string Group(string regex, string groupName = null)
        {
            return groupName == null ? "(" + regex + ")" : "(?<" + groupName + ">" + regex + ")";
        }
    

        public static void ClearContainer(CloudBlobContainer container, string prefix)
        {
            foreach (CloudBlockBlob blob in container.ListBlobs(useFlatBlobListing: true).Where(b => b is CloudBlockBlob).Select(b => b as CloudBlockBlob))
            {
                if (blob.Name.StartsWith(prefix))
                {
                    Console.WriteLine("Deleting blob '" + blob.Uri.ToString() + "'");
                    blob.Delete();
                }
            }
        }

        public static UploadFilesResult UploadFiles(CloudBlobContainer container, string localDirectory, string prefix = "")
        {
            UploadFilesResult ret = new UploadFilesResult();
            localDirectory = localDirectory.Replace("\\", "/");
            foreach (var file in Directory.GetFiles(localDirectory).Select(f => f.Replace("\\", "/")))
            {
                var blobName = ConvertLocalFileToBlobName(localDirectory, file, prefix);
                var blob = UploadFile(container, file, blobName);

                ret.Results.Add(new UploadFileResult() { LocalFile = file, RemoteFile = blob });
            }

            foreach (var child in Directory.GetDirectories(localDirectory))
            {
                var childName = Path.GetFileName(child);
                string newPrefix;

                if (prefix.Length == 0)
                {
                    newPrefix = childName + "/";
                }
                else
                {
                    newPrefix = prefix + childName + "/";
                }

                var nestedResult = UploadFiles(container, child, newPrefix);
                ret.Merge(nestedResult);
            }

            return ret;
        }

        /// <summary>
        /// Converts a local file into a valid Azure Blob name.
        /// </summary>
        /// <param name="localRootDirectory">The full path to the logical root of the local file system.  The portion of the local file path that is common to this root will be stripped out of the blob name.</param>
        /// <param name="localFileFullPath">The full path to a local file</param>
        /// <param name="remotePrefixToPrepend">A prefix that will be prepended to the blob name.  This function will not insert a parth separator '/' in between your prefix and the rest of the blob name.</param>
        /// <returns>a valid blob name that logically matches the input file.</returns>
        public static string ConvertLocalFileToBlobName(string localRootDirectory, string localFileFullPath, string remotePrefixToPrepend)
        {
            var blobName = localFileFullPath.Replace(localRootDirectory, "");

            if (blobName.StartsWith("/"))
            {
                blobName = blobName.Substring(1);
            }

            if (blobName.EndsWith("/"))
            {
                blobName = blobName.Substring(0, blobName.Length - 1);
            }

            blobName = remotePrefixToPrepend + blobName;

            return blobName;
        }

        public static CloudBlockBlob UploadFile(CloudBlobContainer container, string localFilePath, string blobName)
        {
            var blob = container.GetBlockBlobReference(blobName);

            Console.WriteLine("Publishing blob '" + blob.Uri.ToString() + "'");
            blob.UploadFromFile(localFilePath, FileMode.Open);

            if (localFilePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                blob.Properties.ContentType = "text/html";
                blob.SetProperties();
            }
            else if (localFilePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                blob.Properties.ContentType = "text/css";
                blob.SetProperties();
            }
            else if (localFilePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                blob.Properties.ContentType = "application/javascript";
                blob.SetProperties();
            }
            else if (localFilePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                blob.Properties.ContentType = "image/png";
                blob.SetProperties();
            }
            else
            {
                new ConsoleString("Unable to set content type for file " + localFilePath, ConsoleColor.Yellow).WriteLine();
            }
            return blob;
        }
    }
}
