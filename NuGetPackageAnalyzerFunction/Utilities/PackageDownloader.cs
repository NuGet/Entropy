using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NuGetPackageAnalyzerFunction.Utilities
{
    public static class PackageDownloader
    {
        public static async Task<Stream> DownloadAsync(Uri packageUri, TelemetryClient client, ILogger log, CancellationToken cancellationToken)
        {
            Stream packageStream = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Download the package from the network to a temporary file.
                using(var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(packageUri, HttpCompletionOption.ResponseHeadersRead))
                {

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new InvalidOperationException($"Expected status code {HttpStatusCode.OK} for package download, actual: {response.StatusCode}");
                    }

                    using (var networkStream = await response.Content.ReadAsStreamAsync())
                    {
                        packageStream = FileStreamUtility.GetTemporaryFile();

                        await networkStream.CopyToAsync(packageStream, FileStreamUtility.BufferSize, cancellationToken);
                    }
                }

                packageStream.Position = 0;

                stopwatch.Stop();

                client.TrackMetric("PackageDownloadTimeInMS", stopwatch.ElapsedMilliseconds, properties: new Dictionary<string, string>() {
                    { "Url", packageUri.ToString() }
                });

                return packageStream;
            }
            catch (Exception e)
            {
                log.LogError($"Exception thrown when trying to download package from {packageUri}", e);
                packageStream?.Dispose();
                throw;
            }
        }
    }
}
