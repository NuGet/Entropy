using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.GithubEventHandler.Function
{
    public class DeleteOldBlobs
    {
        private readonly IEnvironment _environment;

        public DeleteOldBlobs(IEnvironment environment)
        {
            _environment = environment;
        }

        // Run this function once a day, but there are probably many people who schedule their
        // work at midnight, or hourly, so we'll use a "random" time to reduce risk of competing
        // for shared resources.
        [FunctionName("DeleteOldBlobs")]
        public async Task RunAsync([TimerTrigger("6 12 16 * * *"
#if DEBUG
            , RunOnStartup = true
#endif
            )]TimerInfo myTimer, ILogger log)
        {
            DateOnly deleteBefore = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
            log.LogInformation("Deleting directories older than {0:yyyy-MM-dd}", deleteBefore);

            CloudStorageAccount account = CloudStorageAccount.Parse(_environment.Get("AzureWebJobsStorage"));
            CloudBlobClient client = account.CreateCloudBlobClient();

            // Get only virtual subdirectories of the incoming virtual directory to avoid enumerating many files in directories
            // that are not yet old enough to delete.
            const string prefix = "webhooks/incoming/";
            BlobResultSegment? segment = await client.ListBlobsSegmentedAsync(
                prefix: prefix,
                useFlatBlobListing: false,
                BlobListingDetails.None,
                maxResults: null,
                currentToken: null,
                options: null,
                operationContext: null);

            // In case there's more than one segment of directories, iterate them all
            while (segment?.Results != null)
            {
                foreach (var item in segment.Results)
                {
                    if (item is CloudBlobDirectory directory)
                    {
                        // Parse subdirectory as date, and compare to delete date
                        var lastSegment = directory.Uri.Segments[^1];
                        if (TryParseDate(lastSegment, out DateOnly date))
                        {
                            if (date < deleteBefore)
                            {
                                log.LogInformation("Deleting " + directory.Prefix);
                                await DeleteDirectoryAsync(directory, log);
                            }
                        }
                        else
                        {
                            log.LogInformation("Not deleting " + directory.Prefix);
                        }
                    }
                }

                if (segment.ContinuationToken != null)
                {
                    segment = await client.ListBlobsSegmentedAsync(prefix, segment.ContinuationToken);
                }
                else
                {
                    segment = null;
                }
            }
        }

        /// <summary>Parse a string in format 'yyyy-MM-dd/' as a date.</summary>
        /// <param name="input">The input string to parse</param>
        /// <param name="date">The date, if it could be succesfully parsed. If the string could not be parsed, this output is undefined.</param>
        /// <returns>A boolean, true if parsing was successful, false otherwise.</returns>
        private static bool TryParseDate(string input, out DateOnly date)
        {
            if (input.Length != 11 || input[4] != '-' || input[7] != '-' || input[10] != '/')
            {
                return false;
            }

            var span = input.AsSpan();
            if (!int.TryParse(span.Slice(0, 4), out int year)
                || !int.TryParse(span.Slice(5, 2), out int month)
                || !int.TryParse(span.Slice(8, 2), out int day))
            {
                return false;
            }

            try
            {
                date = new DateOnly(year, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static async Task DeleteDirectoryAsync(CloudBlobDirectory directory, ILogger log)
        {
            var segment = await directory.Container.ListBlobsSegmentedAsync(
                prefix: directory.Prefix,
                useFlatBlobListing: true,
                blobListingDetails: BlobListingDetails.All,
                maxResults: null,
                currentToken: null,
                options: null,
                operationContext: null);

            // Run a number of delete requests in parallel, but running too many in parllal (at
            // least with the local emulator) seems counter productive.
            Task[] tasks = new Task[8];
            int deleted = 0;

            // Iterate all segments, when the number of files is so large it doesn't fit in one segment.
            while (segment?.Results != null)
            {
                using (var enumerator = segment.Results.GetEnumerator())
                {
                    // Ramp up parallel work: fill task list
                    for (int i =0; i < tasks.Length; i++)
                    {
                        if (enumerator.MoveNext())
                        {
                            tasks[i] = ((ICloudBlob)enumerator.Current).DeleteAsync();
                            deleted++;
                        }
                        else
                        {
                            tasks[i] = Task.CompletedTask;
                        }
                    }

                    // steady state parallel work: start one new task when one existing task finishes
                    while (enumerator.MoveNext())
                    {
                        await Task.WhenAny(tasks);

                        for (int i =0; i < tasks.Length; i++)
                        {
                            if (tasks[i].IsCompleted)
                            {
                                tasks[i] = ((ICloudBlob)enumerator.Current).DeleteAsync();
                                deleted++;
                                break;
                            }
                        }
                    }

                    // ramp down parallel work: wait for all remaining tasks to finish.
                    await Task.WhenAll(tasks);
                }

                if (segment.ContinuationToken != null)
                {
                    segment = await directory.Container.ListBlobsSegmentedAsync(segment.ContinuationToken);
                }
                else
                {
                    segment = null;
                }
            }

            log.LogInformation("Deleted " + deleted + " from " + directory.Prefix);
        }
    }
}
