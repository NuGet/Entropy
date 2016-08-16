using System;
using System.IO;
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
        private readonly MyGetFeedBuilder _feedBuilder;
        private readonly HttpSource _httpSource;
        private readonly ILogger _logger;

        public MyGetNuGetSymbolsPackageDownloader(MyGetFeedBuilder myGetFeedBuilder, HttpSource httpSource, ILogger logger)
        {
            _feedBuilder = myGetFeedBuilder;
            _httpSource = httpSource;
            _logger = logger;
        }


        public async Task<bool> IsAvailableAsync(PackageIdentity identity, CancellationToken token)
        {
            var request = new HttpSourceRequest(() =>
            {
                var url = _feedBuilder.GetSymbolsUrl(identity);

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
            var url = _feedBuilder.GetSymbolsUrl(identity);

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
