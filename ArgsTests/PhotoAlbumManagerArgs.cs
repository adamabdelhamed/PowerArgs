using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsTests
{
    public enum OutputType
    {
        Verbose,
        Light,
        Quiet,
    }

    public enum ConflictPolicy
    {
        Throw,
        ServerWins,
        ClientWins,
    }

    [ArgDescription("A program that lets you manage a collection of photos in Azure storage containers.")]
    [ArgExample("photos createalbum -a myaccount -k mykey -c wedding","creates an album called 'wedding' in a container named 'wedding'")]
    public class PhotoAlbumManagerArgs
    {
        [DefaultValue(OutputType.Light)]
        [ArgDescription("How much output to display while performing management operations")]
        public OutputType Output { get; set; }

        [ArgActionMethod]
        [ArgDescription("Creates a new album in the target container, creating the container if it does not exist")]
        public void CreateAlbum(CreateAlbumArgs args) { }

        [ArgActionMethod]
        [ArgExample(@"photos download -a myaccount -k mykey -c wedding -BlobFilePath ceremony.png -LocalFile C:\temp\ceremony.png", "Downloads a blob called ceremony.png to a temp folder on the local machine")]
        [ArgDescription("Downloads a single blob to the local file system.")]
        public void Download(DownloadArgs args) { }

        [ArgActionMethod]
        [ArgExample(@"photos upload -a myaccount -k mykey -c wedding -LocalDirectory C:\images", "Uploads all the files in the local directory to the 'wedding' container")]
        [ArgExample(@"photos upload -a myaccount -k mykey -c wedding -LocalDirectory C:\videos", "Uploads all the files in the local directory to the 'wedding' container")]
        [ArgDescription("Uploads one or more files from the local file system to the target container")]
        public void Upload(UploadArgs args) { }
    }

    public class CreateAlbumArgs : StorageArgs
    {

    }


    public class DownloadArgs : StorageArgs
    {
        [ArgRequired]
        [ArgDescription("The name of a blob to download")]
        public string BlobFilePath { get; set; }

        [ArgDescription("The local path to download to.  This description does not really need to be this long, but I need to test to make sure that the new description formatting feature that cleanly wraps long descriptions is working properly.  This long description should be enough to test that out!")]
        public string LocalFile { get; set; }

        [DefaultValue(ConflictPolicy.Throw)]
        [ArgDescription("What do do if there is a conflict when downloading a file")]
        public ConflictPolicy ConflictPolicy { get; set; }
    }

    public class UploadArgs : StorageArgs
    {
        [ArgRequired(IfNot="LocalDirectory"), ArgExistingFile]
        [ArgDescription("If specified the single file will be uploaded")]
        public string LocalFile { get; set; }

        [ArgRequired(IfNot="LocalFile"), ArgExistingDirectory]
        [ArgCantBeCombinedWith("LocalFile")]
        [ArgDescription("If specified, all files in the directory will be uploaded")]
        public string LocalDirectory { get; set; }

        [DefaultValue(ConflictPolicy.Throw)]
        [ArgDescription("What do do if there is a conflict when uploading a file")]
        public ConflictPolicy ConflictPolicy { get; set; }
    }

    public class StorageArgs
    {
        [ArgRequired, ArgShortcut("-a"), ArgDescription("The storage account to connect to")]
        [ArgPosition(1)]
        public string StorageAccountName { get; set; }

        [ArgRequired, ArgShortcut("-k"), ArgDescription("The storage key to use to connect")]
        [ArgPosition(2)]
        public string StorageAccountKey { get; set; }

        [DefaultValue("DefaultAlbum"), ArgShortcut("-c"), ArgDescription("The storage container to target")]
        [ArgPosition(3)]
        public string Container { get; set; }

        [ArgDescription("Set this flag to use https")]
        public bool UseHttps { get; set; }
    }
}
