using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LogReplay.LogParsing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LogReplay.Commands
{
    public class RunCommand
    {
        public static async Task<int> ExecuteAsync(RunOptions options)
        {
            // Create cancellation token
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            // Connect to Azure storage
            var storageAccount = CloudStorageAccount.Parse(options.ConnectionString);
            var container = storageAccount.CreateCloudBlobClient().GetContainerReference(options.LogContainer);
            if (!container.Exists())
            {
                Trace.TraceError("No such container exists");
                return -1;
            }

            // Determine data range
            var fromPrefix = options.FromPrefix.ToArray();
            var untilPrefix = options.UntilPrefix.ToArray();
            var fromDate = CreateDateTimeOffSet(fromPrefix);
            var untilDate = CreateDateTimeOffSet(untilPrefix);
            if (untilDate <= fromDate)
            {
                untilDate = untilDate.AddHours(1);
            }

            Trace.TraceInformation("Will replay requests in range: {0} - {1}", fromDate, untilDate);
            
            // Build container root
            var containerRoot = GetContainerRoot(options, fromPrefix, untilPrefix);

            // Get the log file blobs that should be replayed
            var blobsToReplay = await GetLogBlobsToReplay(container, containerRoot, fromDate, untilDate);
            if (!blobsToReplay.Any())
            {
                Trace.TraceError("No log files have been found that correspond to the requested from-until range.");
                return -2;
            }
            
            Trace.TraceInformation("Found {0} log files to replay.", blobsToReplay.Count);

            // Run (and log to CSV)
            using (var logFileWriter = new StreamWriter(File.Open(options.LogFile, FileMode.Append, FileAccess.Write)))
            {
                // Write log CSV header
                if (logFileWriter.BaseStream.Position == 0)
                {
                    await logFileWriter.WriteLineAsync("DateTime;Url;OriginalStatusCode;ReplayedStatusCode;OriginalTimeTaken;ReplayedTimeTaken;ReplayedStatusDescription");
                }

                logFileWriter.AutoFlush = true;

                // Perform requests
                var tasks = new List<Task>(blobsToReplay.Count);
                foreach (var temp in blobsToReplay)
                {
                    var blob = temp;
                    var logCsv = logFileWriter;

                    var task = await Task.Factory.StartNew(async () =>
                    {
                        var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromMinutes(1);

                        var stopwatch = new Stopwatch();

                        using (var streamReader = new StreamReader(blob.OpenRead()))
                        {
                            var previousRequestDateTime = DateTimeOffset.MinValue;

                            do
                            {
                                var rawLogLine = streamReader.ReadLine();
                                if (rawLogLine != null)
                                {
                                    var entry = W3CLogEntryParser.ParseLogEntryFromLine(rawLogLine);
                                    if (entry != null)
                                    {
                                        // Make sure the request rate is the same as the request rate in the log.
                                        if (previousRequestDateTime != DateTimeOffset.MinValue)
                                        {
                                            var difference = entry.RequestDateTime - previousRequestDateTime;

                                            await Task.Delay(difference, cancellationTokenSource.Token);
                                        }
                                        previousRequestDateTime = entry.RequestDateTime;

                                        // Run request
                                        var uriBuilder = new UriBuilder(options.Target);
                                        uriBuilder.Path = entry.RequestPath;
                                        uriBuilder.Query = entry.QueryString;

                                        Trace.WriteLine(string.Format("Current request: {0}", rawLogLine), "Verbose");

                                        var referenceTime = DateTime.UtcNow;

                                        stopwatch.Start();

                                        string csvLogLine;
                                        try
                                        {
                                            var response = await httpClient.GetAsync(uriBuilder.Uri, cancellationTokenSource.Token);

                                            stopwatch.Stop();

                                            csvLogLine = string.Format("{0:O};\"{1}\";{2:0};{3:0};{4:0};{5:0};\"{6}\"",
                                                    referenceTime,
                                                    uriBuilder.Uri,
                                                    entry.StatusCode ?? 0,
                                                    (int)response.StatusCode,
                                                    entry.TimeTaken ?? 0,
                                                    (int)stopwatch.Elapsed.TotalMilliseconds,
                                                    !response.IsSuccessStatusCode ? response.ReasonPhrase : string.Empty);

                                            Trace.TraceInformation("{0} {1} ({2:0.00}ms) - {3}", (int)response.StatusCode, response.ReasonPhrase, stopwatch.Elapsed.TotalMilliseconds, uriBuilder.Uri);
                                        }
                                        catch (TaskCanceledException)
                                        {
                                            stopwatch.Stop();

                                            csvLogLine = string.Format("{0:O};\"{1}\";{2:0};{3:0};{4:0};{5:0};\"{6}\"",
                                                    referenceTime,
                                                    uriBuilder.Uri,
                                                    entry.StatusCode ?? 0,
                                                    0,
                                                    entry.TimeTaken ?? 0,
                                                    (int)stopwatch.Elapsed.TotalMilliseconds,
                                                    "Request timeout.");

                                            Trace.TraceError("Timeout ({0:0.00}ms) - {1}", stopwatch.Elapsed.TotalMilliseconds, uriBuilder.Uri);
                                        }
                                        finally
                                        {
                                            stopwatch.Reset();
                                        }

                                        lock (logCsv)
                                        {
                                            logCsv.WriteLine(csvLogLine);
                                        }

                                        Trace.WriteLine(string.Format("Result: {0}", csvLogLine), "Verbose");
                                    }
                                }
                            }
                            while (!streamReader.EndOfStream);
                        }
                    }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    tasks.Add(task);
                }

                // Wait for either a Console.ReadLine() or completion of all requests
                Task.WaitAny(
                    Task.Factory.StartNew(() =>
                    {
                        Console.ReadLine();
                        cancellationTokenSource.Cancel(false);
                    }, cancellationTokenSource.Token), 
                    
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            Task.WaitAll(tasks.ToArray());
                        }
                        catch (Exception)
                        {
                            if (!cancellationTokenSource.IsCancellationRequested)
                            {
                                throw;
                            }
                        }
                    }, cancellationTokenSource.Token)
                );
            }

            return 0;
        }

        private static string GetContainerRoot(RunOptions options, string[] fromPrefix, string[] untilPrefix)
        {
            // We can be smart in listing blobs if the from - to range is relatively small
            var containerRoot = options.LogContainerRoot ?? string.Empty;
            if (!string.IsNullOrEmpty(containerRoot))
            {
                containerRoot += "/";
            }

            for (int i = 0; i < Math.Min(fromPrefix.Length, untilPrefix.Length); i++)
            {
                if (fromPrefix[i] == untilPrefix[i])
                {
                    containerRoot += string.Format("{0}/", fromPrefix[i]);
                }
            }
            return containerRoot;
        }

        private static async Task<List<CloudBlockBlob>> GetLogBlobsToReplay(CloudBlobContainer container, string containerRoot, DateTimeOffset fromDate, DateTimeOffset untilDate)
        {
            // Get the log file blobs that should be replayed
            var blobsToReplay = new List<CloudBlockBlob>();
            BlobContinuationToken continuationToken = null;
            do
            {
                var segment = await container.ListBlobsSegmentedAsync(containerRoot,
                    useFlatBlobListing: true,
                    blobListingDetails: BlobListingDetails.Metadata,
                    maxResults: null,
                    currentToken: continuationToken,
                    options: null,
                    operationContext: null);

                continuationToken = segment.ContinuationToken;

                foreach (var blob in segment.Results)
                {
                    var cloudBlockBlob = blob as CloudBlockBlob;
                    if (cloudBlockBlob != null && cloudBlockBlob.Properties.LastModified.HasValue)
                    {
                        // Check if the blob timestamp falls within our prefix range
                        var timestamp = cloudBlockBlob.Properties.LastModified.Value;
                        if (timestamp >= fromDate && timestamp <= untilDate)
                        {
                            blobsToReplay.Add(cloudBlockBlob);
                        }
                    }
                }
            } while (continuationToken != null);
            return blobsToReplay;
        }


        private static DateTimeOffset CreateDateTimeOffSet(string[] values)
        {
            var year = 2000;
            var month = 1;
            var day = 1;
            var hour = 0;

            for (int i = 0; i < values.Length; i++)
            {
                int intValue;
                if (int.TryParse(values[i], out intValue))
                {
                    switch (i)
                    {
                        case 0:
                            year = intValue;
                            break;
                        case 1:
                            month = intValue;
                            break;
                        case 2:
                            day = intValue;
                            break;
                        case 3:
                            hour = intValue;
                            break;
                    }
                }
            }

            return new DateTimeOffset(year, month, day, hour, 0, 0, TimeSpan.Zero);
        }
    }
}