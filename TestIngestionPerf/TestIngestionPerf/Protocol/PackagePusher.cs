using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using PackageDependency = NuGet.Packaging.Core.PackageDependency;
using PackageDependencyGroup = NuGet.Packaging.PackageDependencyGroup;

namespace TestIngestionPerf
{
    public class PackagePusher
    {
        private readonly HttpClient _httpClient;
        private readonly string _pushEndpoint;

        public PackagePusher(HttpClient httpClient, string pushEndpoint)
        {
            _httpClient = httpClient;
            _pushEndpoint = pushEndpoint;
        }

        public TestPackage GeneratePackage(string idPattern, string versionPattern)
        {
            var ticks = $"{DateTimeOffset.Now.UtcTicks:D20}";
            var id = string.Format(idPattern, ticks);
            var version = NuGetVersion.Parse(string.Format(versionPattern, ticks));

            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                var packageBuilder = new PackageBuilder();

                packageBuilder.Id = id;
                packageBuilder.Version = version;
                packageBuilder.Authors.Add("Joel Verhagen");
                packageBuilder.Description = "Test package.";
                packageBuilder.DependencyGroups.Add(
                    new PackageDependencyGroup(
                        NuGetFramework.Parse("net45"),
                        new PackageDependency[]
                        {
                            new PackageDependency("Newtonsoft.Json", VersionRange.Parse("9.0.1"))
                        }));

                packageBuilder.Save(memoryStream);
                bytes = memoryStream.ToArray();
            }

            return new TestPackage(id, version, bytes);
        }

        public async Task PushPackageAsync(string apiKey, byte[] bytes)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Put, _pushEndpoint))
            {
                request.Headers.Add("X-NuGet-ApiKey", apiKey);
                request.Headers.Add("X-NuGet-Protocol-Version", "4.1.0");
                request.Content = new ByteArrayContent(bytes);

                using (var response = await _httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
