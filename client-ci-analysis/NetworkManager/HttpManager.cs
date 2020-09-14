using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkManager
{
    public sealed class HttpManager : IDisposable
    {
        private HttpClient _httpClient;
        private Dictionary<string, AuthenticationHeaderValue> _credentials;

        private HttpManager(HttpManagerOptions options)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(options.UserAgent);
            _credentials = new Dictionary<string, AuthenticationHeaderValue>();
        }

        public static async Task<HttpManager> CreateAsync(HttpManagerOptions options)
        {
            if (options.UserAgent == null)
            {
                throw new ArgumentException();
            }

            if (options.CacheDirectory == null)
            {
                throw new ArgumentException();
            }

            if (!Directory.Exists(options.CacheDirectory))
            {
                Directory.CreateDirectory(options.CacheDirectory);
            }

            return new HttpManager(options);
        }

        public void AddCredential(string urlPrefix, AuthenticationHeaderValue authHeader)
        {
            _credentials.Add(urlPrefix, authHeader);
        }

        public async Task<FileStream> GetAsync(string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var cacheFile = new Uri(Path.GetTempPath());
            cacheFile = new Uri(cacheFile, "client-ci-analysis/");
            cacheFile = new Uri(cacheFile, request.RequestUri.Host + "/");
            cacheFile = new Uri(cacheFile, request.RequestUri.LocalPath.TrimStart('/'));
            var cacheFullPath = cacheFile.LocalPath;

            if (!File.Exists(cacheFullPath))
            {
                var cacheDirectory = Path.GetDirectoryName(cacheFullPath);
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                if (TryGetAuthenticationHeader(url, out AuthenticationHeaderValue authHeader))
                {
                    request.Headers.Authorization = authHeader;
                }

                Console.WriteLine("Requesting " + url);
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var cacheStream = File.OpenWrite(cacheFullPath))
                {
                    await responseStream.CopyToAsync(cacheStream);
                }
            }

            var stream = File.OpenRead(cacheFullPath);
            return stream;
        }

        private bool TryGetAuthenticationHeader(string url, [MaybeNullWhen(false)] out AuthenticationHeaderValue? authHeader)
        {
            foreach (var (prefix, auth) in _credentials)
            {
                if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    authHeader = auth;
                    return true;
                }
            }

            authHeader = null;
            return false;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }


    }
}
