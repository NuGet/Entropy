using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace FixDevV3Blobs
{
    class Program
    {
        const int MaxTasks = 32;

        const string ProcessedListFilename = "processed.txt";
        private static readonly HashSet<string> ProcessedBlobs = new HashSet<string>();
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

            var blobList = await LoadBlobListAsync(container);

            Log($"Got {blobList.Count} blobs");
            var blobCount = blobList.Count;

            var stopwatch = await ProcessBlobListAsync(blobList, search, replace);

            Log(
                "Processed {0} entries in {1}, processing speed: {2} entries per hour, {3} per 1M. Used {4} tasks.",
                blobCount,
                stopwatch.Elapsed,
                blobCount / stopwatch.Elapsed.TotalHours,
                TimeSpan.FromTicks(stopwatch.Elapsed.Ticks * (1000000 / blobCount)),
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

        private static async Task<ConcurrentBag<CloudBlockBlob>> LoadBlobListAsync(CloudBlobContainer container)
        {
            var blobList = new ConcurrentBag<CloudBlockBlob>();

            BlobContinuationToken continuationToken = null;
            var stopwatch = Stopwatch.StartNew();
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
                    throw new InvalidOperationException("Unexpected result item type: " + segment.Results.First(r => r.GetType() != typeof(CloudBlockBlob)).GetType());
                }
                range.ForEach(b => blobList.Add(b));
                continuationToken = segment.ContinuationToken;
                Log($"Discovered {blobList.Count} blobs in {stopwatch.Elapsed}");
            } while (continuationToken != null);
            stopwatch.Stop();
            return blobList;
        }

        private static async Task<Stopwatch> ProcessBlobListAsync(ConcurrentBag<CloudBlockBlob> blobList, string search, string replace)
        {
            var stopwatch = Stopwatch.StartNew();
            var count = 0;
            var skipped = 0;
            var blobCount = blobList.Count;

            var tasks = Enumerable.Range(1, MaxTasks).Select(async _ => 
            {
                await Task.Yield();
                while (blobList.TryTake(out var blob))
                {
                    var curCount = Interlocked.Increment(ref count);
                    if (Processed(blob))
                    {
                        Interlocked.Increment(ref skipped);
                        continue;
                    }
                    await ProcessBlobAsync(blob, search, replace);
                    if ((curCount % 1000) == 0)
                    {
                        Log(
                            "{0}/{1} (skipped: {2}) {3}, ETA: {4}",
                            curCount,
                            blobCount,
                            skipped,
                            stopwatch.Elapsed,
                            TimeSpan.FromSeconds(((double)blobCount - curCount) / curCount * stopwatch.Elapsed.TotalSeconds));
                    }
                }
            });
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            return stopwatch;
        }

        private static bool Processed(CloudBlockBlob blob)
        {
            return ProcessedBlobs.Contains(blob.Uri.AbsoluteUri);
        }

        private static async Task ProcessBlobAsync(CloudBlockBlob blob, string search, string replace)
        {
            var stopwatch = Stopwatch.StartNew();
            var content = await blob.DownloadTextAsync(
                Encoding.UTF8,
                AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag),
                options: null,
                operationContext: null);
            stopwatch.Stop();

            //SaveContent("input", blob, content);

            var newContent = content.Replace(search, replace);

            //SaveContent("output", blob, newContent);

            // imitate upload back
            //await Task.Delay(stopwatch.Elapsed);

            if (newContent != content)
            {
                await blob.UploadTextAsync(
                    newContent,
                    Encoding.UTF8,
                    AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag),
                    options: null,
                    operationContext: null);
            }
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

        private static void Log(string format, params object[] args)
        {
            Console.WriteLine("[{0:hh:mm:ss}] {1}", DateTime.Now, string.Format(format, args));
        }
    }
}
