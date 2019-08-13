using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SearchScorer.Common;

namespace SearchScorer
{
    class Program
    {
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            ServicePointManager.DefaultConnectionLimit = 64;

            var assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var settings = new SearchScorerSettings
            {
                ControlBaseUrl = "https://api-v2v3search-0.nuget.org/",
                TreatmentBaseUrl = "https://azuresearch-usnc.nuget.org/",
                FeedbackSearchQueriesCsvPath = Path.Combine(assemblyDir, "FeedbackSearchQueries.csv"),
                CuratedSearchQueriesCsvPath = Path.Combine(assemblyDir, "CuratedSearchQueries.csv"),
                TopSearchQueriesCsvPath = @"C:\Users\jver\Desktop\search-scorer\TopSearchQueries-2019-08-05.csv",
                TopSearchSelectionsCsvPath = @"C:\Users\jver\Desktop\search-scorer\TopSearchSelections-2019-08-05.csv",
                TopSearchSelectionsV2CsvPath = @"C:\Users\jver\Desktop\search-scorer\TopSearchSelectionsV2-2019-08-05.csv",
                GoogleAnalyticsSearchReferralsCsvPath = @"C:\Users\jver\Desktop\search-scorer\GoogleAnalyticsSearchReferrals-2019-07-03-2019-08-04.csv",
                GitHubUsageJsonPath = @"C:\Users\jver\Desktop\search-scorer\GitHubUsage.v1-2019-08-06.json",
                GitHubUsageCsvPath = @"C:\Users\jver\Desktop\search-scorer\GitHubUsage.v1-2019-08-06.csv",
            };

            // Output data in more convenient formats.
            GitHubUsageCsvWriter.Write(
                settings.GitHubUsageCsvPath,
                GitHubUsageJsonReader.Read(settings.GitHubUsageJsonPath));
            TopSearchSelectionsV2CsvWriter.Write(
                settings.TopSearchSelectionsV2CsvPath,
                TopSearchSelectionsCsvReader.Read(settings.TopSearchSelectionsCsvPath));

            using (var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient())
            {
                var searchClient = new SearchClient(httpClient);
                var validator = new PackageIdPatternValidator(searchClient);

                // Verify all desired package IDs exist.
                var feedback = FeedbackSearchQueriesCsvReader
                    .Read(settings.FeedbackSearchQueriesCsvPath)
                    .SelectMany(x => x.MostRelevantPackageIds);
                var curated = CuratedSearchQueriesCsvReader
                    .Read(settings.CuratedSearchQueriesCsvPath)
                    .SelectMany(x => x.PackageIdToScore.Keys);
                Console.WriteLine("Searching for non-existent package IDs");
                var allPackageIds = feedback.Concat(curated);
                var nonExistentPackageIds = await validator.GetNonExistentPackageIdsAsync(allPackageIds, settings);
                Console.WriteLine();
                Console.WriteLine($"Found {nonExistentPackageIds.Count}.");
                foreach (var packageId in nonExistentPackageIds)
                {
                    Console.WriteLine($" - {packageId}");
                }

                await new IREvalutation.RelevancyScoreEvaluator(searchClient).RunAsync(settings);
                // await Feedback.FeedbackEvaluator.RunAsync(httpClient, settings);
            }
        }
    }
}
