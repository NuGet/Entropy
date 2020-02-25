using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NuGet.Protocol.Catalog;
using NuGet.Versioning;
using Newtonsoft.Json;

namespace TestIngestionPerf
{
    public class FlatContainerChecker : IEndpointChecker
    {
        private readonly SimpleHttpClient _simpleHttpClient;
        private readonly string _endpoint;

        public FlatContainerChecker(
            SimpleHttpClient simpleHttpClient,
            string endpoint)
        {
            _simpleHttpClient = simpleHttpClient;
            _endpoint = endpoint.TrimEnd('/');
        }

        public string Name => $"Flat Container: {_endpoint}";

        public async Task<bool> DoesPackageExistAsync(string id, NuGetVersion version)
        {
            var url = $"{_endpoint}/{id.ToLowerInvariant()}/index.json";
            var result = await _simpleHttpClient.DeserializeUrlAsync<VersionList>(url);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            var versionList = result.GetResultOrThrow();

            var versionMatch = versionList
                .Versions
                .FirstOrDefault(x => NuGetVersion.Parse(x) == version);
            if (versionMatch == null)
            {
                return false;
            }

            return true;
        }

        private class VersionList
        {
            [JsonProperty("versions")]
            public List<string> Versions { get; set; }
        }
    }
}
