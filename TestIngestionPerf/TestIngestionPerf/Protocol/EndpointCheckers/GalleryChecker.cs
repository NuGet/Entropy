using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace TestIngestionPerf
{
    public class GalleryChecker : IEndpointChecker
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public GalleryChecker(
            HttpClient httpClient,
            string baseUrl)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public string Name => $"Gallery: {_baseUrl}";

        public async Task<bool> DoesPackageExistAsync(string id, NuGetVersion version)
        {
            var url = $"{_baseUrl}/packages/{id}/{version.ToNormalizedString()}";
            using (var response = await _httpClient.GetAsync(url))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();

                if (html.IndexOf("<small class=\"text-nowrap\">" + version.ToNormalizedString(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
