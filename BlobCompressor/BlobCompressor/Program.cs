using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobCompressor
{
    class Program
    {
        static void Main(string[] args)
        {
            // Validate arguments
            if (args.Length != 4)
            {
                PrintUsage();
                return;
            }

            string sourceConnectionString = args[0];
            string sourceContainerName = args[1];
            string destinationConnectionString = args[2];
            string destinationContainerName = args[3];

            CloudStorageAccount sourceAccount;
            if (!CloudStorageAccount.TryParse(sourceConnectionString, out sourceAccount))
            {
                Console.WriteLine("Error: could not parse source connection string.");
                PrintUsage();
                return;
            }

            CloudStorageAccount destinationAccount;
            if (!CloudStorageAccount.TryParse(destinationConnectionString, out destinationAccount))
            {
                Console.WriteLine("Error: could not parse destination connection string.");
                PrintUsage();
                return;
            }

            // ServicePointManager optimizations
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;

            // Run
            Run(sourceAccount, sourceContainerName, destinationAccount, destinationContainerName);
        }

        private static void Run(CloudStorageAccount sourceAccount, string sourceContainerName, CloudStorageAccount destinationAccount, string destinationContainerName)
        {
            var sourceBlobClient = sourceAccount.CreateCloudBlobClient();
            var sourceContainer = sourceBlobClient.GetContainerReference(sourceContainerName);
            sourceContainer.CreateIfNotExists();

            var destinationBlobClient = destinationAccount.CreateCloudBlobClient();
            var destinationContainer = destinationBlobClient.GetContainerReference(destinationContainerName);
            destinationContainer.CreateIfNotExists();

            destinationContainer.SetPermissions(new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            });

            BlobContinuationToken continuationToken = null;
            do
            {
                var segment = sourceContainer.ListBlobsSegmented(null, true, BlobListingDetails.Metadata, null, continuationToken, null, null);
                continuationToken = segment.ContinuationToken;

                var segmentCount = segment.Results.Count();

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                Console.WriteLine("Start processing {0} blobs...", segmentCount);

                Parallel.ForEach(segment.Results, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
                    item =>
                    {
                        var cloudBlockBlob = item as CloudBlockBlob;
                        if (cloudBlockBlob != null)
                        {
                            var blobAsText = cloudBlockBlob.DownloadText();

                            blobAsText = ReplacePartialUrls(blobAsText, sourceContainerName, destinationContainerName);
                            
                            using (var compressed = new MemoryStream())
                            {
                                using (var gzip = new StreamWriter(new GZipStream(compressed, CompressionMode.Compress, true)))
                                {
                                    gzip.Write(blobAsText);
                                }
                                
                                compressed.Position = 0;

                                var destinationBlob = destinationContainer.GetBlockBlobReference(cloudBlockBlob.Name);

                                destinationBlob.Properties.ContentEncoding = "gzip";
                                destinationBlob.Properties.ContentType = cloudBlockBlob.Properties.ContentType;
                                destinationBlob.Properties.CacheControl = cloudBlockBlob.Properties.CacheControl;

                                destinationBlob.UploadFromStream(compressed);
                            }
                        }
                    });

                stopWatch.Stop();
                Console.WriteLine("Processed {0} blobs. ({1:0.00} blob(s)/second)", segmentCount, segmentCount / stopWatch.Elapsed.TotalSeconds);
            }
            while (continuationToken != null);
        }

        private static string ReplacePartialUrls(string blobAsText, string sourceContainerName, string destinationContainerName)
        {
            var replace = sourceContainerName;
            if (replace.StartsWith("v3-"))
            {
                replace = replace.Substring(3);
            }

            var with = destinationContainerName;
            if (with.StartsWith("v3-"))
            {
                with = with.Substring(3);
            }

            return blobAsText.Replace(replace, with);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  blobcompressor <source-connectionstring> <source-container> <destination-connectionstring> <destination-container>");
        }
    }
}
