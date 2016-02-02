using NuGet.Services.Metadata.Catalog.Persistence;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.Test.Server
{
    public class StorageHttpMessageHandler : HttpMessageHandler
    {
        Storage _storage;

        public StorageHttpMessageHandler(Storage storage)
        {
            _storage = storage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            StorageContent content = await _storage.Load(request.RequestUri, cancellationToken);
            if (content == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            else
            {
                HttpContent httpContent = new StreamContent(content.GetContentStream());
                httpContent.Headers.ContentType = new MediaTypeHeaderValue(content.ContentType);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = httpContent };
            }
        }
    }
}
