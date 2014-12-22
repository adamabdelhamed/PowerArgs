using Hammer.AzureStorage;
using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs;
using System;

namespace DoxygenPublisher
{
    public class StorageAccountArgs
    {
        [ArgRequired(PromptIfMissing = true), StickyArg, ArgDescription("The name of the azure storage account to target"), ArgDefaultValue("adamabdelhamed2")]
        public string StorageAccountName { get; set; }

        [ArgRequired(PromptIfMissing = true), StickyArg, ArgDescription("The key of the azure storage account to target")]
        public string StorageAccountKey { get; set; }
    }

    public class StorageContainerScopedArgs : StorageAccountArgs
    {
        [ArgRequired(PromptIfMissing = true), StickyArg, ArgDescription("The azure storage container to target"), ArgDefaultValue("powerargsdocs")]
        public string ContainerName { get; set; }

        private Lazy<CloudBlobContainer> _container;

        [ArgIgnore]
        public CloudBlobContainer Container { get { return _container.Value; } }

        public StorageContainerScopedArgs()
        {
            _container = new Lazy<CloudBlobContainer>(GetTargetContainerReferenceSafe);
        }

        public virtual CloudBlobContainer GetTargetContainerReferenceSafe()
        {
            var ret = SharedStorageAccount.BlobClient.GetContainerReference(ContainerName);
            ret.CreateIfNotExists();
            ret.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Blob });
            return ret;
        }
    }
}
