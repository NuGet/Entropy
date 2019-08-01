using System.Net;
using System.Threading.Tasks;
using SearchScorer.Feedback;

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

            var controlBaseUrl = "https://api-v2v3search-0.nuget.org/";
            var treatmentBaseUrl = "https://azuresearch-usnc.nuget.org/";

            await FeedbackEvaluator.RunAsync(controlBaseUrl, treatmentBaseUrl);
        }
    }
}
