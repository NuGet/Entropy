using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using SearchScorer.Common;
using Humanizer;

namespace SearchScorer
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 64;

            var assemblyDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var settings = new SearchScorerSettings
            {
                ControlBaseUrl = "https://azuresearch-usnc.nuget.org/",
                TreatmentBaseUrl = "http://localhost:21751/",
                // TreatmentBaseUrl = "https://azuresearch-usnc-perf.nuget.org/",

                FeedbackSearchQueriesCsvPath = Path.Combine(assemblyDir, "FeedbackSearchQueries.csv"),
                CuratedSearchQueriesCsvPath = Path.Combine(assemblyDir, "CuratedSearchQueries.csv"),
                ClientCuratedSearchQueriesCsvPath = Path.Combine(assemblyDir, "ClientCuratedSearchQueries.csv"),
                TopSearchQueriesCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\TopSearchQueries-90d-organic-2020-10-19.csv",
                TopClientSearchQueriesCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\TopClientSearchQueries-45d-2020-10-19.csv",
                GoogleAnalyticsSearchReferralsCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\GoogleAnalyticsSearchReferrals-empty.csv",

                // Used for the "convert-csv" command
                TopSearchSelectionsCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\TopSearchSelections-90d-2020-10-19.csv",
                GitHubUsageJsonPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\GitHubUsage.v1-2019-08-06.json",
                GitHubUsageCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\GitHubUsage.v1-2019-08-06.csv",
                TopSearchSelectionsV2CsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\TopSearchSelectionsV2-90d-2020-10-19.csv",

                // Used for the "hash-queries" command.
                TopV3SearchQueriesPathPattern = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\TopV3SearchQueries-90d-p*-2020-10-19.csv",
                HasherKeyFile = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\HasherKey.txt",
                HashedSearchQueryLookupCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\HashedSearchQueries-2020-10-19.csv",

                // The following settings are only necessary if running the "probe" command.
                AzureSearchServiceName = "",
                AzureSearchIndexName = "",
                AzureSearchApiKey = "",
                ProbeResultsCsvPath = @"C:\Users\jver\OneDrive - Microsoft\search-scorer\ProbeResults.csv",

                PackageIdWeights = CreateRange(lower: 1, upper: 10, increments: 3),
                TokenizedPackageIdWeights = CreateRange(lower: 1, upper: 10, increments: 3),
                TagsWeights = CreateRange(lower: 1, upper: 10, increments: 3),
                DownloadWeights = CreateRange(lower: 1000, upper: 30000, increments: 5000),
            }; 

            using (var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient())
            {
                if (args.Length == 0 || args[0] == "score")
                {
                    await RunScoreCommandAsync(settings, httpClient);
                }
                else if (args[0] == "probe")
                {
                    await RunProbeCommandAsync(settings, httpClient);
                }
                else if (args[0] == "curation-coverage")
                {
                    ShowCurationCoverage(settings);
                }
                else if (args[0] == "convert-csv")
                {
                    WriteConvenientCsvs(settings);
                }
                else if (args[0] == "verify-package-ids")
                {
                    await VerifyPackageIdsExistAsync(settings, httpClient);
                }
                else if (args[0] == "hash-queries")
                {
                    HashQueries(settings);
                }
                else if (args[0] == "compare")
                {
                    var searchTerm = string.Join(" ", args.Skip(1).ToArray());
                    await CompareSearchTermAsync(settings, httpClient, searchTerm);
                }
            }
        }

        private static async Task CompareSearchTermAsync(SearchScorerSettings settings, HttpClient httpClient, string searchTerm)
        {
            Console.WriteLine($"Search term: {searchTerm}");

            var searchClient = new SearchClient(httpClient);
            var take = 10;
            Console.WriteLine($"Searching on control {settings.ControlBaseUrl}");
            var control = await searchClient.SearchAsync(settings.ControlBaseUrl, searchTerm, take);
            Console.WriteLine($"Searching on treatment {settings.TreatmentBaseUrl}");
            var treatment = await searchClient.SearchAsync(settings.TreatmentBaseUrl, searchTerm, take);
            Console.WriteLine();

            var maxControl = GetColumnWidth("Control", control);
            var maxTreatment = GetColumnWidth("Treatment", control);

            Console.Write("Rank | ");
            Console.Write(DisplayHeading("Control", control).PadRight(maxControl));
            Console.Write(" | ");
            Console.Write(DisplayHeading("Treatment", treatment).PadRight(maxTreatment));
            Console.WriteLine();

            Console.Write("---- | ");
            Console.Write(new string('-', maxControl));
            Console.Write(" | ");
            Console.Write(new string('-', maxTreatment));
            Console.WriteLine();

            for (var i = 0; i < control.Data.Count || i < treatment.Data.Count; i++)
            {
                Console.Write((i + 1).ToString().PadRight("Rank".Length));
                Console.Write(" | ");
                Console.Write(DisplayPackage(control.Data.ElementAtOrDefault(i)).PadRight(maxControl));
                Console.Write(" | ");
                Console.Write(DisplayPackage(treatment.Data.ElementAtOrDefault(i)).PadRight(maxTreatment));
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        private static int GetColumnWidth(string label, SearchResponse control)
        {
            return control.Data.Select(x => DisplayPackage(x)).Concat(new[] { DisplayHeading(label, control) }).Max(x => x.Length);
        }

        private static string DisplayHeading(string label, SearchResponse control)
        {
            return $"{label} ({DisplayNumber(control.TotalHits)} hits)";
        }

        private static string DisplayPackage(SearchResult x)
        {
            if (x == null)
            {
                return string.Empty;
            }

            return $"{x.Id} ({DisplayNumber(x.Debug.Document.TotalDownloadCount)})";
        }

        private static string DisplayNumber(double number)
        {
            return MetricNumeralExtensions.ToMetric(number, decimals: 2);
        }

        private static void ShowCurationCoverage(SearchScorerSettings settings)
        {
            Console.WriteLine("Search query curation");
            Console.WriteLine("=====================");
            ShowCurationCoverage(
                TopSearchQueriesCsvReader.Read(settings.TopSearchQueriesCsvPath),
                CuratedSearchQueriesCsvReader.Read(settings.CuratedSearchQueriesCsvPath));

            Console.WriteLine();

            Console.WriteLine("Client search query curation");
            Console.WriteLine("============================");
            ShowCurationCoverage(
                TopClientSearchQueriesCsvReader.Read(settings.TopClientSearchQueriesCsvPath),
                CuratedSearchQueriesCsvReader.Read(
                    settings.ClientCuratedSearchQueriesCsvPath,
                    settings.CuratedSearchQueriesCsvPath));
        }

        private static void ShowCurationCoverage(IReadOnlyDictionary<string, int> topSearchQueries, IReadOnlyList<CuratedSearchQuery> curatedSearchQueries)
        {
            var curatedSearchQueriesSet = curatedSearchQueries
                .Select(x => x.SearchQuery)
                .ToHashSet();

            float totalCount = topSearchQueries.Sum(x => x.Value);
            var uncurated = topSearchQueries.Where(x => !curatedSearchQueriesSet.Contains(x.Key));
            var uncuratedCount = uncurated.Sum(x => x.Value);
            Console.WriteLine($"Curation coverage: {(totalCount - uncuratedCount) / totalCount:P2}");

            const int topN = 10;
            var top = uncurated.OrderByDescending(x => x.Value).Take(topN).ToList();
            Console.WriteLine($"Top {top.Count} uncurated:");
            foreach (var query in top)
            {
                Console.WriteLine($"  - {query.Key} ({query.Value}, {query.Value / totalCount:P2})");
            }
        }

        private static void HashQueries(SearchScorerSettings settings)
        {
            Console.WriteLine("Reading hasher key file...");
            var hasherKey = File.ReadAllText(settings.HasherKeyFile).Trim();
            Console.WriteLine("Reading search queries...");
            var searchQueries = TopV3SearchQueriesCsvReader.Read(settings.TopV3SearchQueriesPathPattern);
            Console.WriteLine("Writing hashed search queries...");
            HashedSearchQueryCsvWriter.Write(hasherKey, settings.HashedSearchQueryLookupCsvPath, searchQueries);
        }

        private static async Task RunScoreCommandAsync(SearchScorerSettings settings, HttpClient httpClient)
        {
            var searchClient = new SearchClient(httpClient);
            var scoreEvaluator = new IREvalutation.RelevancyScoreEvaluator(searchClient);
            await scoreEvaluator.RunAsync(settings);
        }

        private static async Task RunProbeCommandAsync(SearchScorerSettings settings, HttpClient httpClient)
        {
            var credentials = new SearchCredentials(settings.AzureSearchApiKey);
            var azureSearchClient = new SearchServiceClient(settings.AzureSearchServiceName, credentials);

            var index = await azureSearchClient.GetNuGetSearchIndexAsync(settings);

            Console.WriteLine("Running {0} tests.", GetProbeTests(settings).Count());

            foreach (var test in GetProbeTests(settings))
            {
                var searchClient = new SearchClient(httpClient);
                var scoreEvaluator = new IREvalutation.RelevancyScoreEvaluator(searchClient);

                // Update the Azure Search index
                await azureSearchClient.UpdateNuGetSearchIndexAsync(
                    settings,
                    index,
                    test.PackageIdWeight,
                    test.TokenizedPackageIdWeight,
                    test.TagsWeight,
                    test.DownloadScoreBoost);

                // Score the new index.
                var report = await scoreEvaluator.GetCustomVariantReportAsync(
                    settings,
                    customVariantUrl: settings.TreatmentBaseUrl);

                // Save the result to the output path
                SearchProbesCsvWriter.Append(
                    settings.ProbeResultsCsvPath,
                    new SearchProbesRecord
                    {
                        PackageIdWeight = test.PackageIdWeight,
                        TokenizedPackageIdWeight = test.TokenizedPackageIdWeight,
                        TagsWeight = test.TagsWeight,
                        DownloadScoreBoost = test.DownloadScoreBoost,

                        CuratedSearchScore = report.CuratedSearchQueries.Score,
                        ClientCuratedSearchScore = report.ClientCuratedSearchQueries.Score,
                        FeedbackScore = report.FeedbackSearchQueries.Score
                    });
            }
        }

        private static IEnumerable<SearchProbeTest> GetProbeTests(SearchScorerSettings settings)
        {
            var fields = new[]
            {
                settings.PackageIdWeights,
                settings.TokenizedPackageIdWeights,
                settings.TagsWeights,
                settings.DownloadWeights
            };

            return CartesianProduct(fields)
                .Select(x =>
                {
                    var values = x.ToList();

                    return new SearchProbeTest
                    {
                        PackageIdWeight = values[0],
                        TokenizedPackageIdWeight = values[1],
                        TagsWeight = values[2],
                        DownloadScoreBoost = values[3]
                    };
                });
        }

        private static IReadOnlyList<double> CreateRange(int lower, int upper, double increments)
        {
            var count = (upper - lower) / increments + 1;
            return Enumerable
                .Range(0, (int)count)
                .Select(i => (i * increments + lower))
                .ToList();
        }

        // From: https://codereview.stackexchange.com/questions/122699/finding-a-cartesian-product-of-multiple-lists
        private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };

            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) => accumulator.SelectMany(
                    accseq => sequence,
                    (accseq, item) => accseq.Concat(new[] { item })));
        }

        private static void WriteConvenientCsvs(SearchScorerSettings settings)
        {
            // Output data in more convenient formats.
            GitHubUsageCsvWriter.Write(
                settings.GitHubUsageCsvPath,
                GitHubUsageJsonReader.Read(settings.GitHubUsageJsonPath));
            TopSearchSelectionsV2CsvWriter.Write(
                settings.TopSearchSelectionsV2CsvPath,
                TopSearchSelectionsCsvReader.Read(settings.TopSearchSelectionsCsvPath));
        }

        private static async Task VerifyPackageIdsExistAsync(SearchScorerSettings settings, HttpClient httpClient)
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

            var clientCurated = CuratedSearchQueriesCsvReader
                .Read(settings.ClientCuratedSearchQueriesCsvPath)
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
        }
    }

}
