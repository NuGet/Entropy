using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Registration;

namespace TestIngestionPerf
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            ServicePointManager.DefaultConnectionLimit = 64;

            var idPattern = "Jver.TestPackage.IngestionTime4";
            var versionPattern = "0.0.1-v{0}";
            var testDuration = TimeSpan.FromMinutes(60);
            var packageCount = 240;
            var outputFile = $"results-{DateTimeOffset.UtcNow:yyyyMMddHHmmssFFFFFFF}.csv";

            var pushEndpoint = "https://dev.nugettest.org/api/v2/package";
            var apiKey = "";
            var galleryEndpoints = new[]
            {
                "https://dev.nugettest.org",
            };
            var flatContainerEndpoints = new[]
            {
                "https://apidev.nugettest.org/v3-flatcontainer",
                "https://nugetgallerydev.blob.core.chinacloudapi.cn/v3-flatcontainer"
            };
            var registrationEndpoints = new[]
            {
                "https://apidev.nugettest.org/v3/registration3",
                "https://apidev.nugettest.org/v3/registration3-gz",
                "https://apidev.nugettest.org/v3/registration3-gz-semver2",
                "https://nugetgallerydev.blob.core.chinacloudapi.cn/v3-registration3",
                "https://nugetgallerydev.blob.core.chinacloudapi.cn/v3-registration3-gz",
                "https://nugetgallerydev.blob.core.chinacloudapi.cn/v3-registration3-gz-semver2",
            };
            var searchEndpoints = new[]
            {
                "https://nuget-dev-usnc-v2v3search.nugettest.org/query",
                "https://nuget-dev-ussc-v2v3search.nugettest.org/query",
                "https://nuget-dev-eastasia-search.nugettest.org/query",
                "https://nuget-dev-southeastasia-search.nugettest.org/query",
            };
            var expandableSearchEndpoints = new string[0];
            var packageCheckFrequency = 15;

            //var pushEndpoint = "https://int.nugettest.org/api/v2/package";
            //var apiKey = "";
            //var galleryEndpoints = new[]
            //{
            //    "https://int.nugettest.org",
            //};
            //var flatContainerEndpoints = new[]
            //{
            //    "https://apiint.nugettest.org/v3-flatcontainer",
            //};
            //var registrationEndpoints = new[]
            //{
            //    "https://apiint.nugettest.org/v3/registration3",
            //    "https://apiint.nugettest.org/v3/registration3-gz",
            //    "https://apiint.nugettest.org/v3/registration3-gz-semver2",
            //};
            //var searchEndpoints = new[]
            //{
            //    "http://localhost:21751/query",
            //};
            //var expandableSearchEndpoints = new[]
            //{
            //    "https://nuget-usnc-v2v3search.int.nugettest.org/query",
            //    "https://nuget-ussc-v2v3search.int.nugettest.org/query",
            //};

            var loggerFactory = new LoggerFactory().AddConsole();
            var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip };
            var httpClient = new HttpClient(httpClientHandler);
            var simpleHttpClient = new SimpleHttpClient(httpClient, loggerFactory.CreateLogger<SimpleHttpClient>());
            var registrationClient = new RegistrationClient(simpleHttpClient);
            var portTester = new PortTester();
            var portDiscovery = new SimplePortDiscoverer(portTester);
            var portExpander = new PortExpander(portDiscovery);
            var packagePusher = new PackagePusher(httpClient, pushEndpoint);
            var packageChecker = new PackageChecker(loggerFactory.CreateLogger<PackageChecker>());
            var tester = new Tester(packageChecker, loggerFactory.CreateLogger<Tester>());

            var endpointCheckers = new List<IEndpointChecker>();

            endpointCheckers.AddRange(galleryEndpoints
                .Select(endpoint => new GalleryChecker(httpClient, endpoint))
                .ToList());

            endpointCheckers.AddRange(flatContainerEndpoints
                .Select(endpoint => new FlatContainerChecker(simpleHttpClient, endpoint))
                .ToList());

            endpointCheckers.AddRange(registrationEndpoints
                .Select(endpoint => new RegistrationChecker(registrationClient, endpoint))
                .ToList());

            endpointCheckers.AddRange(searchEndpoints
                .Select(endpoint => new SearchChecker(simpleHttpClient, endpoint))
                .ToList());

            endpointCheckers.AddRange(expandableSearchEndpoints
                .Select(endpoint => new SearchChecker(simpleHttpClient, endpoint, portExpander))
                .ToList());

            var writeLock = new object();
            AppendLine(writeLock, outputFile, GetCsvHeader(endpointCheckers));

            var testParameters = new TestParameters
            {
                PackagePusher = packagePusher,
                ApiKey = apiKey,
                IdPattern = idPattern,
                VersionPattern = versionPattern,
                PackageCheckFrequency = packageCheckFrequency,
                EndpointCheckers = endpointCheckers,
                TestDuration = testDuration,
                PackageCount = packageCount,
                OnPackageResult = x => AppendResult(writeLock, outputFile, x),
            };

            var results = await tester.ExecuteAsync(testParameters);

            Console.WriteLine(GetCsvHeader(endpointCheckers));
            foreach (var result in results)
            {
                Console.WriteLine(GetCsvLine(result));
            }
        }

        private static string GetCsvHeader(IReadOnlyList<IEndpointChecker> endpointCheckers)
        {
            return string.Join(",", new object[]
            {
                "Started",
                "Id",
                "Version",
                "Push Duration",
            }.Concat(endpointCheckers
                .Select(x => x.Name)
                .Cast<object>()));
        }

        private static string GetCsvLine(PackageResult packageResult)
        {
            return string.Join(",", new object[]
            {
                packageResult.Started.ToUniversalTime().ToString("O"),
                packageResult.Package.Id,
                packageResult.Package.Version.ToNormalizedString(),
                packageResult.PushDuration.TotalSeconds,
            }.Concat(packageResult
                .EndpointResults
                .Select(x => x.Duration.TotalSeconds)
                .Cast<object>()));
        }

        private static void AppendResult(object writeLock, string outputFile, PackageResult packageResult)
        {
            AppendLine(writeLock, outputFile, GetCsvLine(packageResult));
        }

        private static void AppendLine(object writeLock, string outputFile, string line)
        {
            lock (writeLock)
            {
                File.AppendAllLines(outputFile, new[] { line });
            }
        }
    }
}
