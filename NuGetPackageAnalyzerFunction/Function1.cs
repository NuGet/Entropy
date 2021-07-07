using Microsoft.ApplicationInsights;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NuGetPackageAnalyzerFunction
{
    public static class Function1
    {
        /// <summary>
        /// Rename the FunctionName to something apropriate if you are making changes
        /// so that the functions are deployed correctly and not accidentally overwrite the previous ones.
        /// </summary>
        /// <param name="message">Service bus queue message object</param>
        /// <param name="log">The logger that logs to application insights</param>
        /// <returns>Task</returns>
        [FunctionName("TimestampV1Analyzer")]
        public static async Task RunAsync([ServiceBusTrigger("allpackages-items",
                Connection = "ServiceBus_String")]CatalogLeafMessage message,
        ILogger log)
        {
            var appInsights = GetTelemetryClient();

            await MessageProcessor.Process(message, appInsights, log);

            appInsights.Flush();
        }

        private static TelemetryClient GetTelemetryClient()
        {
            var telemetryClient = new TelemetryClient();
            telemetryClient.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
            telemetryClient.Context.Operation.Id = Guid.NewGuid().ToString();
            return telemetryClient;
        }
    }
}
