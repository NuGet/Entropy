using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NuGetPackageAnalyzerFunction
{
    public static class MessageProcessor
    {

        public static async Task Process(CatalogLeafMessage message, TelemetryClient telemetry, ILogger log)
        {
            try
            {
                if (!IsValidMessage(message))
                {
                    throw new Exception("Invalid message specified");
                }

                log.LogInformation($"Received Message: {message.Id}/{message.Version}");

                await Analyzer.Analyze(
                    message,
                    telemetry,
                    log);
            }
            catch (Exception ex)
            {
                log.LogError("Failed to process message!", ex);
            }
        }

        private static bool IsValidMessage(CatalogLeafMessage message)
        {
            // Message should contain ID and Version
            return message != null
                && !string.IsNullOrWhiteSpace(message.Id)
                && !string.IsNullOrWhiteSpace(message.Version);
        }
    }
}
