using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace StagingWebApi
{
    public class AzurePackageStorage : PackageStorageBase
    {
        public AzurePackageStorage()
        {
            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["StorageConnectionString"].ConnectionString;
        }

        public string ConnectionString
        {
            get;
            set;
        }

        public override async Task Delete(Uri location)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlockBlob blob = new CloudBlockBlob(location, account.Credentials);
            await blob.DeleteIfExistsAsync();
        }

        public override async Task<Uri> Save(Stream stream, string blobName, string contentDisposition, string contentType)
        {
            stream.Seek(0, SeekOrigin.Begin);

            CloudStorageAccount account = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("packages");

            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            blob.Properties.ContentDisposition = contentDisposition;
            blob.Properties.ContentType = contentType;

            await blob.UploadFromStreamAsync(stream);

            return blob.Uri;
        }
    }
}