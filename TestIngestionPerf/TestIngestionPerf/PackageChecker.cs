using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using NuGet.Versioning;

namespace TestIngestionPerf
{
    public class PackageChecker
    {
        private readonly ILogger<PackageChecker> _logger;

        public PackageChecker(ILogger<PackageChecker> logger)
        {
            _logger = logger;
        }

        public async Task<IReadOnlyList<EndpointResult>> GetEndpointResultsAsync(
            IReadOnlyList<IEndpointChecker> endpointCheckers,
            string id,
            NuGetVersion version)
        {
            var stopwatch = Stopwatch.StartNew();

            var tasks = endpointCheckers
                .Select(async packageChecker =>
                {
                    await Task.Yield();
                    TimeSpan? duration = null;
                    do
                    {
                        var exists = await packageChecker.DoesPackageExistAsync(id, version);

                        if (!exists)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        else
                        {
                            duration = stopwatch.Elapsed;
                            _logger.LogInformation(
                                "After {Duration}, {Id} {Version} is available on {Name}.",
                                duration,
                                id,
                                version,
                                packageChecker.Name);
                        }
                    }
                    while (!duration.HasValue);

                    return duration.Value;
                })
                .ToList();

            var durations = await Task.WhenAll(tasks);

            return endpointCheckers
                .Zip(durations, (k, v) => new EndpointResult(k, v))
                .ToList();
        }
    }
}
