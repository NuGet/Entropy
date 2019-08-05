using System.IO;
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
                FeedbackSearchQueryCsvPath = Path.Combine(assemblyDir, "FeedbackSearchQueries.csv"),
                TopSearchQueryCsvPath = @"C:\Users\jver\Desktop\search-scorer\TopQueries-2019-08-03.csv",
                TopSearchSelectionsCsvPath = @"C:\Users\jver\Desktop\search-scorer\TopSearchSelections-2019-08-03.csv",
            };

            using (var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip })
            using (var httpClient = new HttpClient())
            {
                var searchClient = new SearchClient(httpClient);

                await new IREvalutation.RelevancyScoreEvaluator(searchClient).RunAsync(settings);
                // await Feedback.FeedbackEvaluator.RunAsync(httpClient, settings);
            }
        }
    }
}
