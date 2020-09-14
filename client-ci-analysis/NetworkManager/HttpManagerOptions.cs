using System.Net.Http.Headers;

namespace NetworkManager
{
    public record HttpManagerOptions
    {
        public ProductInfoHeaderValue UserAgent { get; init; }

        public string CacheDirectory { get; init; }

        public HttpManagerOptions(ProductInfoHeaderValue userAgent, string cacheDirectory)
        {
            UserAgent = userAgent;
            CacheDirectory = cacheDirectory;
        }
    }
}
