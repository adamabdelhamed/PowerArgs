using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DoxygenPublisher
{
    [ArgActions]
    public class ListDoxygenDocsVersionsCommand
    {
        [ArgActionMethod, ArgDescription("Lists all versions of PowerArgs docs published to the given container")]
        public static void ListDoxygenDocsVersions(StorageContainerScopedArgs args)
        {
            Console.WriteLine("Searching for known docs versions...");
            List<string> knownVersion = new List<string>();
            foreach (CloudBlockBlob blob in args.Container.ListBlobs(useFlatBlobListing: true).Where(b => b is CloudBlockBlob).Select(b => b as CloudBlockBlob))
            {
                var blobFirstSegment = blob.Name.Substring(0, blob.Name.IndexOf("/"));
                if(knownVersion.Contains(blobFirstSegment) == false)
                {
                    ConsoleString.WriteLine("    "+blobFirstSegment, ConsoleColor.Cyan);
                    knownVersion.Add(blobFirstSegment);
                }
            }
        } 
    }
}

