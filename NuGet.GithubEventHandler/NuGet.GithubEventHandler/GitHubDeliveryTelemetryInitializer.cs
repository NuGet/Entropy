using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace NuGet.GithubEventHandler
{
    internal class GitHubDeliveryTelemetryInitializer : ITelemetryInitializer
    {
        private IHttpContextAccessor _contextAccessor;
        private const string XGitHubDelivery = "X-GitHub-Delivery";

        public GitHubDeliveryTelemetryInitializer(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is RequestTelemetry requestTelemetry)
            {
                HttpContext httpContext = _contextAccessor.HttpContext;
                string? deliveryId = httpContext.Request.Headers[XGitHubDelivery].FirstOrDefault();
                if (deliveryId != null)
                {
                    requestTelemetry.Properties[XGitHubDelivery] = deliveryId;
                }
            }
        }
    }
}
