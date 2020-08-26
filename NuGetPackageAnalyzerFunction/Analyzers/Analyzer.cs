using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NuGetPackageAnalyzerFunction
{
    public static class Analyzer
    {
        /// <summary>
        /// Add a new analyzer and invoke your analyzer here
        /// </summary>
        /// <param name="message">The message object with your expected queue message format</param>
        /// <param name="telemetry">The app insights telemetry client</param>
        /// <param name="log">Information logger, this gets written to the App insights</param>
        /// <returns>Task</returns>
        public static async Task Analyze(CatalogLeafMessage message, TelemetryClient telemetry, ILogger log)
        {
            await TimestampV1Analyzer.Process(
                message,
                telemetry,
                log);
        }
    }
}
