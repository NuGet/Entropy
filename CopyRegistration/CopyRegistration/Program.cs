using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Registration;
using NuGetGallery;

namespace CopyRegistration
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            const string connectionString = "DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=***";
            const string cursorValue = "2019-04-29T12:19:24.1091293";
            const string newBaseContainerName = "v3-registration4";
            const string newBaseUrl = "https://mystorageaccount.blob.core.windows.net/" + newBaseContainerName;

            ServicePointManager.DefaultConnectionLimit = 64;

            var loggerFactory = new LoggerFactory().AddConsole();
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
            var httpClient = new HttpClient(httpClientHandler);
            var simpleHttpClient = new SimpleHttpClient(httpClient, loggerFactory.CreateLogger<SimpleHttpClient>());
            var registrationClient = new RegistrationClient(simpleHttpClient);
            var copier = new RegistrationHiveCopier(
                registrationClient,
                simpleHttpClient,
                loggerFactory.CreateLogger<RegistrationHiveCopier>());

            var cloudBlobClient = new CloudBlobClientWrapper(connectionString, readAccessGeoRedundant: false);

            var hives = new[]
            {
                new
                {
                    OldBaseUrl = "https://api.nuget.org/v3/registration3",
                    NewBaseUrl = newBaseUrl,
                    NewContainerName = newBaseContainerName,
                    Gzip = false,
                    Cursor = true,
                },
                new
                {
                    OldBaseUrl = "https://api.nuget.org/v3/registration3-gz",
                    NewBaseUrl = newBaseUrl + "-gz",
                    NewContainerName = newBaseContainerName + "-gz",
                    Gzip = true,
                    Cursor = true,
                },
                new
                {
                    OldBaseUrl = "https://api.nuget.org/v3/registration3-gz-semver2",
                    NewBaseUrl = newBaseUrl + "-gz-semver2",
                    NewContainerName = newBaseContainerName + "-gz-semver2",
                    Gzip = true,
                    Cursor = false,
                },
            };

            var ids = new[]
            {
                "DDPlanet.Logging",
                "IBA.ECL.Services.Shared.Enums",
                "Lith.FlatFile",
                "Momentum.Pm.Api",
                "Momentum.Pm.PortalApi",
                "MSBuild.Obfuscar",
                "Sensus",
                "TIKSN-Framework",
                "Vostok.ServiceDiscovery",
            };

            var hiveTasks = hives
                .Select(async hive =>
                {
                    await Task.Yield();

                    var container = cloudBlobClient.GetContainerReference(hive.NewContainerName);
                    await container.CreateIfNotExistAsync();

                    var idTasks = ids
                        .Select(async id =>
                        {
                            await Task.Yield();
                            await copier.CopyAsync(
                                container,
                                hive.OldBaseUrl,
                                hive.NewBaseUrl,
                                id,
                                hive.Gzip);
                        })
                        .ToList();
                    await Task.WhenAll(idTasks);

                    if (hive.Cursor)
                    {
                        var cursorBlob = container.GetBlobReference("cursor.json");
                        cursorBlob.Properties.ContentType = "application/json";
                        var cursorJObject = new JObject();
                        cursorJObject["value"] = cursorValue;
                        var cursorJson = cursorJObject.ToString(Formatting.Indented);
                        var cursorBytes = Encoding.UTF8.GetBytes(cursorJson);
                        using (var memoryStream = new MemoryStream(cursorBytes))
                        {
                            await cursorBlob.UploadFromStreamAsync(memoryStream, overwrite: true);
                        }
                    }
                })
                .ToList();
            await Task.WhenAll(hiveTasks);
        }
    }
}
