using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ci_testfailure_analyzer.Models.AzDO;
using System.Linq;

namespace ci_testfailure_analyzer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Expected at least 1 argument, got " + args.Length);
                return;
            }

            if (File.Exists(args[0]))
            {
                Console.WriteLine("Expected '{0}' to be a directory, but found a file", args[0]);
                return;
            }

            var cache = new DirectoryInfo(args[0]);
            if (!cache.Exists)
            {
                cache.Create();
            }

            var buildDefinition = 8118; //private build. Use 8117 for official, 14219 for trusted build.

            if (args.Length > 1 && int.TryParse(args[1], out buildDefinition))
            { }

            Console.WriteLine($"Cache path: {cache.FullName}");
            Console.WriteLine($"CI pipeline Build Definition: {buildDefinition}");

            var accountName = Environment.GetEnvironmentVariable("AzDO_ACCOUNT");

            // You can get bearer token from web browser use here, had no time to fix automatically getting it.
            var bearerToken = Environment.GetEnvironmentVariable("AzDO_BEARERTOKEN");
            if (string.IsNullOrEmpty(bearerToken))
            {
                try
                {
                    // Bearer token expire after few minutes so save in easy to update txt file.
                    string filename = Path.Combine(cache.FullName, "bearerToken.txt");
                    Console.WriteLine($"Reading bearer token from {filename}");
                    string[] lines = File.ReadLines(filename).ToArray();

                    bearerToken = lines[0];
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }

            if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(bearerToken))
            {
                Console.WriteLine("Set AzDO_ACCOUNT and AzDO_BEARERTOKEN (or update bearerToken.txt) environment variables.");
                Console.WriteLine("Project properties -> Debug in VS");
                Console.WriteLine("launchSettings.json in VSCode");
                return;
            }

            var httpClient = CreateAzureDevOpsClient(accountName, bearerToken);

            List<BuildInfo> builds = await BuildFetcher.DownloadBuildsAsync(cache, httpClient, buildDefinition);
            List<CsvRow> rows = await BuildFetcher.GetFailingFunctionalTestAsync(builds, httpClient);
            BuildFetcher.WriteCVSFile(rows, cache, buildDefinition);

            Console.WriteLine("-----End-----");
        }

        private static HttpClient CreateAzureDevOpsClient(string accountName, string bearerToken)
        {
            var httpClient = new HttpClient();

            var cred = new AuthenticationHeaderValue("BASIC", Convert.ToBase64String(Encoding.ASCII.GetBytes(accountName + ":" + bearerToken)));
            httpClient.DefaultRequestHeaders.Authorization = cred;

            var userAgent = new ProductInfoHeaderValue("NugetClientCiAnalysis", "0.1");
            httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);

            return httpClient;
        }
    }
}
