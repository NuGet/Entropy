using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;

namespace MyGetMirror
{
    public class MyGetNuGetSymbolsPackageDownloader : INuGetSymbolsPackageDownloader
    {
        private const int BufferSize = 4096;
        private readonly string _baseUrl;
        private readonly HttpSource _httpSource;
        private readonly ILogger _logger;

        public MyGetNuGetSymbolsPackageDownloader(string source, HttpSource httpSource, ILogger logger)
        {
            _baseUrl = GetBaseUrl(source);
            _httpSource = httpSource;
            _logger = logger;
        }

        private static string GetBaseUrl(string source)
        {
            // Verify the hostname is MyGet.
            Uri uri;
            if (!Uri.TryCreate(source, UriKind.Absolute, out uri) ||
                (!StringComparer.OrdinalIgnoreCase.Equals(uri.Host, "myget.org") &&
                 !uri.Host.EndsWith(".myget.org", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"To download MyGet symbols packages, the source '{source}' must be a HTTP MyGet source.");
            }

            string baseUrl;

            if (source.EndsWith("index.json", StringComparison.OrdinalIgnoreCase))
            {
                // V3 examples:
                // - Public or auth: https://www.myget.org/F/nugetbuild/api/v3/index.json
                // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY/api/v3/index.json
                //
                // We need to strip off the last three pieces, e.g. "api/v3/index.json".
                var pieces = source
                    .Split('/')
                    .Reverse()
                    .Skip(3)
                    .Reverse();

                baseUrl = string.Join("/", pieces).TrimEnd('/');
            }
            else
            {
                // V2 examples:
                // - Public or auth: https://www.myget.org/F/nugetbuild/api/v2
                // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY/api/v2
                //
                // We need to strip off the last two pieces, e.g. "api/v2".
                var pieces = source
                    .TrimEnd('/')
                    .Split('/')
                    .Reverse()
                    .Skip(2)
                    .Reverse();

                baseUrl = string.Join("/", pieces).TrimEnd('/');
            }

            // Output examples:
            // - Public or auth: https://www.myget.org/F/nugetbuild
            // - Pre-auth:       https://www.myget.org/F/nuget-private-test/auth/API-KEY

            return baseUrl;
        }

        private string GetUrl(PackageIdentity identity)
        {
            // Example:
            // https://www.myget.org/F/nugetbuild/symbols/NuGet.ApplicationInsights.Owin/3.0.2633-r-master
            return $"{_baseUrl}/symbols/{identity.Id}/{identity.Version}";
        }

        public async Task<bool> IsAvailableAsync(PackageIdentity identity, CancellationToken token)
        {
            var request = new HttpSourceRequest(() =>
            {
                var url = GetUrl(identity);

                // Ideally this would be an HTTP HEAD request, but MyGet does not seem to support
                // this. As an alternative, make an HTTP GET range request for 0 bytes.
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Range = new RangeHeaderValue(0, 0);

                return requestMessage;
            });

            request.IgnoreNotFounds = true;

            var output = await _httpSource.ProcessStreamAsync(
                request,
                stream =>
                {
                    var isAvailable = stream != null;
                    return Task.FromResult(isAvailable);
                },
                _logger,
                token);

            return output;
        }

        public async Task<T> ProcessAsync<T>(PackageIdentity identity, Func<SymbolsPackageResult, Task<T>> processAsync, CancellationToken token)
        {
            var url = GetUrl(identity);

            var request = new HttpSourceRequest(url, _logger);
            request.IgnoreNotFounds = true;

            var output = await _httpSource.ProcessStreamAsync(
                request,
                async stream =>
                {
                    if (stream == null)
                    {
                        var result = new SymbolsPackageResult
                        {
                            IsAvailable = false,
                            Stream = Stream.Null
                        };

                        return await processAsync(result);
                    }
                    else
                    {
                        // Save the symbols package to a temporary location.
                        var temporaryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                        using (var fileStream = new FileStream(
                            temporaryPath,
                            FileMode.Create,
                            FileAccess.ReadWrite,
                            FileShare.None,
                            BufferSize,
                            FileOptions.Asynchronous | FileOptions.DeleteOnClose))
                        {
                            await stream.CopyToAsync(fileStream);

                            fileStream.Position = 0;

                            var result = new SymbolsPackageResult
                            {
                                IsAvailable = true,
                                Stream = fileStream
                            };

                            return await processAsync(result);
                        }
                    }
                },
                _logger,
                token);

            return output;
        }
    }
}
