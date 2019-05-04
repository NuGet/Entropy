using System;
using System.Collections.Generic;
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

            var pushEndpoint = "https://dev.nugettest.org/api/v2/package";
            var apiKey = "";
            var galleryEndpoints = new[]
            {
                "https://dev.nugettest.org",
            };
            var flatContainerEndpoints = new[]
            {
                "https://apidev.nugettest.org/v3-flatcontainer",
            };
            var registrationEndpoints = new[]
            {
                "https://apidev.nugettest.org/v3/registration3",
                "https://apidev.nugettest.org/v3/registration3-gz",
                "https://apidev.nugettest.org/v3/registration3-gz-semver2",
            };
            var searchEndpoints = new string[0];
            var expandableSearchEndpoints = new[]
            {
                "https://nuget-dev-usnc-v2v3search.nugettest.org/query",
                "https://nuget-dev-ussc-v2v3search.nugettest.org/query",
            };

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

            var testParameters = new TestParameters
            {
                PackagePusher = packagePusher,
                ApiKey = apiKey,
                IdPattern = idPattern,
                VersionPattern = versionPattern,
                EndpointCheckers = endpointCheckers,
                TestDuration = testDuration,
                PackageCount = packageCount,
            };

            var results = await tester.ExecuteAsync(testParameters);

            Console.WriteLine(string.Join(",", new object[]
            {
                "Started",
                "Id",
                "Version",
                "Push Duration",
            }.Concat(testParameters
                .EndpointCheckers
                .Select(x => x.Name)
                .Cast<object>())));

            foreach (var result in results)
            {
                Console.WriteLine(string.Join(",", new object[]
                {
                    result.Started.ToUniversalTime().ToString("O"),
                    result.Package.Id,
                    result.Package.Version.ToNormalizedString(),
                    result.PushDuration.TotalSeconds,
                }.Concat(result
                    .EndpointResults
                    .Select(x => x.Duration.TotalSeconds)
                    .Cast<object>())));
            }
        }
    }
}
