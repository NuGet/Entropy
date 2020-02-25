using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Protocol.Catalog;
using NuGet.Services.AzureSearch.SearchService;
using NuGet.Versioning;

namespace TestIngestionPerf
{
    public class SearchChecker : IEndpointChecker
    {
        private readonly PortExpander _portExpander;
        private readonly ISimpleHttpClient _simpleHttpClient;
        private readonly string _endpoint;

        public SearchChecker(
            ISimpleHttpClient simpleHttpClient,
            string endpoint)
        {
            _simpleHttpClient = simpleHttpClient;
            _endpoint = endpoint;
        }

        public SearchChecker(
            ISimpleHttpClient simpleHttpClient,
            string endpoint,
            PortExpander portExpander)
        {
            _portExpander = portExpander;
            _simpleHttpClient = simpleHttpClient;
            _endpoint = endpoint;
        }

        public string Name => $"Search: {_endpoint}";

        public async Task<bool> DoesPackageExistAsync(string id, NuGetVersion version)
        {
            var mainUrl = $"{_endpoint}?q=packageid:{id}&prerelease=true&semVerLevel=2.0.0";
            var urls = new List<string> { mainUrl };

            if (_portExpander != null)
            {
                var expandedUrls = await _portExpander.ExpandSequentialOpenPortsAsync(44301, urls);
                urls.AddRange(expandedUrls);
            }

            var tasks = urls
                .Select(url => DoesPackageExistAsync(url, id, version))
                .ToList();

            var results = await Task.WhenAll(tasks);

            return results.All(exists => exists);
        }

        private async Task<bool> DoesPackageExistAsync(string url, string id, NuGetVersion version)
        {
            await Task.Yield();

            ResponseAndResult<V3SearchResponse> result;
            try
            {
                result = await _simpleHttpClient.DeserializeUrlAsync<V3SearchResponse>(url);
            }
            catch (HttpRequestException)
            {
                return false;
            }

            var response = result.GetResultOrThrow();

            if (!response.Data.Any())
            {
                return false;
            }

            var idMatch = response
                .Data
                .FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(id, x.Id));
            if (idMatch == null)
            {
                return false;
            }

            var versionMatch = idMatch
                .Versions
                .FirstOrDefault(x => NuGetVersion.Parse(x.Version) == version);
            if (versionMatch == null)
            {
                return false;
            }

            return true;
        }
    }
}
