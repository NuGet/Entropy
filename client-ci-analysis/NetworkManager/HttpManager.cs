using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkManager
{
    public sealed class HttpManager : IDisposable
    {
        private HttpClient _httpClient;
        private Dictionary<string, AuthenticationHeaderValue> _credentials;
        private string _root;

        private HttpManager(HttpManagerOptions options)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(options.UserAgent);
            _credentials = new Dictionary<string, AuthenticationHeaderValue>();
            _root = options.CacheDirectory;
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

        public Task<FileStream> GetAsync(string url, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAsync(url, TimeSpan.MaxValue, cancellationToken);
        }

        public async Task<FileStream> GetAsync(string url, TimeSpan maxCacheAge, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            string cacheFullPath;

            using (var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(url));
                var stringChars = new char[hashBytes.Length * 2];
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    char GetChar(byte b)
                    {
                        if (b > 16) throw new InvalidOperationException();
                        if (b < 10) return (char)('0' + b);
                        else return (char)('a' - 10 + b);
                    }

                    stringChars[i * 2] = GetChar((byte)(hashBytes[i] >> 4));
                    stringChars[i * 2 + 1] = GetChar((byte)(hashBytes[i] & 0x0F));
                }

                cacheFullPath = Path.Combine(_root, new string(stringChars));
            }

            var fileInfo = new FileInfo(cacheFullPath);

            if (!fileInfo.Exists || (DateTime.UtcNow - fileInfo.LastWriteTimeUtc) > maxCacheAge)
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
                using (var cacheStream = fileInfo.OpenWrite())
                {
                    await responseStream.CopyToAsync(cacheStream);
                    cacheStream.SetLength(cacheStream.Position);
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
