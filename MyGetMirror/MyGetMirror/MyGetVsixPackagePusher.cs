using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;

namespace MyGetMirror
{
    public class MyGetVsixPackagePusher : IVsixPackagePusher
    {
        private readonly string _destinationApiKey;
        private readonly HttpSource _httpSource;
        private readonly ILogger _logger;
        private readonly string _pushUrl;

        public MyGetVsixPackagePusher(string pushUrl, string destinationApiKey, HttpSource httpSource, ILogger logger)
        {
            _pushUrl = pushUrl;
            _destinationApiKey = destinationApiKey;
            _httpSource = httpSource;
            _logger = logger;
        }

        public async Task PushAsync(Stream packageStream, CancellationToken token)
        {
            await _httpSource.ProcessResponseAsync(
               new HttpSourceRequest(() =>
               {
                   // Retries should seek the package stream to the beginning.
                   packageStream.Position = 0;

                   var packageContent = new StreamContent(packageStream)
                   {
                       Headers =
                       {
                           ContentType = MediaTypeHeaderValue.Parse("application/octet-stream")
                       }
                   };

                   var content = new MultipartFormDataContent();
                   content.Add(packageContent, "package", "package.vsix");

                   var request = HttpRequestMessageFactory.Create(
                       HttpMethod.Post,
                       _pushUrl,
                       new HttpRequestMessageConfiguration(logger: _logger, promptOn403: false));
                   request.Content = content;
                   request.Headers.TransferEncodingChunked = true;
                   request.Headers.Add("X-NuGet-ApiKey", _destinationApiKey);

                   return request;
               }),
               response =>
               {
                   response.EnsureSuccessStatusCode();
                   return Task.FromResult(0);
               },
               _logger,
               token);
        }
    }
}
