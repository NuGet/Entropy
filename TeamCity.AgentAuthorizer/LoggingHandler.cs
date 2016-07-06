using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.TeamCity.AgentAuthorizer
{
    public class LoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  {request.Method} {request.RequestUri}");
            var response = await base.SendAsync(request, cancellationToken);
            Console.WriteLine($"  {(int)response.StatusCode} {response.ReasonPhrase} {request.RequestUri}");
            return response;
        }
    }
}
