using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace FixDevV3Blobs
{
    class Program
    {
        const int MaxTasks = 32;

        const string ProcessedListFilename = "processed.txt";
        private static HashSet<string> ProcessedBlobs = new HashSet<string>();
        private static object ProcessedOutputLock = new object();

        static async Task<int> Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = MaxTasks * 2 + 1;

            const string connectionString = "<nope>";
            const string containerName = "v3-catalog0";
            const string search = "az635243.vo.msecnd.net/v3-catalog0";
            const string replace = "apidev.nugettest.org/v3/catalog0";

            var account = CloudStorageAccount.Parse(connectionString);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);

            LoadProcessedList();

            var blobList = await LoadBlobList(container);

            Console.WriteLine("Got {0} blobs", blobList.Count);

            var stopwatch = await ProcessBlobList(blobList, search, replace);

            Console.WriteLine(
                "Processed {0} entries in {1}, processing speed: {2} entries per hour, {3} per 1M. Used {4} tasks.",
                blobList.Count,
                stopwatch.Elapsed,
                blobList.Count / stopwatch.Elapsed.TotalHours,
                TimeSpan.FromTicks(stopwatch.Elapsed.Ticks * (1000000 / blobList.Count)),
                MaxTasks);

            return 0;
        }

        private static void LoadProcessedList()
        {
            if (File.Exists(ProcessedListFilename))
            {
                var lines = File.ReadAllLines(ProcessedListFilename);
                ProcessedBlobs.UnionWith(lines);
            }
        }

        private static async Task<List<CloudBlockBlob>> LoadBlobList(CloudBlobContainer container)
        {
            var blobList = new List<CloudBlockBlob>();

            BlobContinuationToken continuationToken = null;
            var stopwatch = Stopwatch.StartNew();
            var n = 5;
            do
            {
                var segment = await container.ListBlobsSegmentedAsync(
                    prefix: null,
                    useFlatBlobListing: true,
                    blobListingDetails: BlobListingDetails.None,
                    maxResults: 5000,
                    currentToken: continuationToken,
                    options: null,
                    operationContext: null);

                var range = segment.Results.OfType<CloudBlockBlob>().ToList();
                if (range.Count != segment.Results.Count())
                {
                    throw new Exception("Unexpected result item type: " + segment.Results.First(r => r.GetType() != typeof(CloudBlockBlob)).GetType());
                }
                blobList.AddRange(range);
                continuationToken = segment.ContinuationToken;
                Console.WriteLine("Discovered {0} blobs in {1}", blobList.Count, stopwatch.Elapsed);
            } while (--n > 0); //(continuationToken != null);
            stopwatch.Stop();
            return blobList;
        }

        private static async Task<Stopwatch> ProcessBlobList(List<CloudBlockBlob> blobList, string search, string replace)
        {
            var stopwatch = Stopwatch.StartNew();
            var tasks = Enumerable.Range(1, MaxTasks).Select(_ => Task.CompletedTask).ToArray();
            var count = 0;
            var skipped = 0;
            foreach (var blob in blobList)
            {
                ++count;
                //await ProcessBlob(blob);
                if (Processed(blob))
                {
                    ++skipped;
                    continue;
                }
                await StartProcessing(tasks, blob, search, replace);
                if ((count % 1000) == 0)
                {
                    Console.WriteLine(
                        "{0}/{1} (skipped: {4}) {2}, ETA: {3}",
                        count,
                        blobList.Count,
                        stopwatch.Elapsed,
                        TimeSpan.FromSeconds(((double)blobList.Count - count) / count * stopwatch.Elapsed.TotalSeconds),
                        skipped);
                }
            }
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            return stopwatch;
        }

        private static bool Processed(CloudBlockBlob blob)
        {
            return ProcessedBlobs.Contains(blob.Uri.AbsoluteUri);
        }

        private static async Task StartProcessing(Task[] tasks, CloudBlockBlob blob, string search, string replace)
        {
            await Task.WhenAny(tasks);

            for (int i = 0; i < tasks.Length; ++i)
            {
                if (tasks[i].IsCompleted)
                {
                    tasks[i] = ProcessBlob(blob, search, replace);
                    return;
                }
            }

            throw new Exception("Failed to find completed task");
        }

        private static async Task ProcessBlob(CloudBlockBlob blob, string search, string replace)
        {
            var stopwatch = Stopwatch.StartNew();
            var content = await blob.DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag), null, null);
            stopwatch.Stop();

            //SaveContent("input", blob, content);

            content = content.Replace(search, replace);

            //SaveContent("output", blob, content);

            // imitate upload back
            await Task.Delay(stopwatch.Elapsed);

            //await blob.UploadTextAsync(content, Encoding.UTF8, AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag), null, null);
            MarkBlobProcessed(blob);
        }

        private static void SaveContent(string prefix, CloudBlockBlob blob, string content)
        {
            var name = Path.Combine(prefix, blob.Name);
            var fi = new FileInfo(name);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            File.WriteAllText(fi.FullName, content);
        }

        private static void MarkBlobProcessed(CloudBlockBlob blob)
        {
            var uri = blob.Uri.AbsoluteUri;
            lock (ProcessedOutputLock)
            {
                File.AppendAllLines(ProcessedListFilename, new[] { uri });
            }
        }
    }
}
