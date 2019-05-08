using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TestIngestionPerf
{
    public class Tester
    {
        private readonly PackageChecker _packageChecker;
        private readonly ILogger<Tester> _logger;

        public Tester(PackageChecker packageChecker, ILogger<Tester> logger)
        {
            _packageChecker = packageChecker;
            _logger = logger;
        }

        public async Task<IReadOnlyList<PackageResult>> ExecuteAsync(TestParameters parameters)
        {
            var ticksBetweenPush = parameters.TestDuration.Ticks / parameters.PackageCount;
            var durationBetweenPush = TimeSpan.FromTicks(ticksBetweenPush);
            _logger.LogInformation(
                "Pushing {PackageCount} packages and waiting {TestDuration} between each push. The test will take at least {TestDuration}.",
                parameters.PackageCount,
                durationBetweenPush,
                parameters.TestDuration);

            var tasks = new List<Task<PackageResult>>();
            for (var number = 1; number <= parameters.PackageCount; number++)
            {
                _logger.LogInformation("Starting package {Number}/{Total}.", number, parameters.PackageCount);
                tasks.Add(TestPackageAsync(number, parameters.PackageCount, parameters));
                
                if (number < parameters.PackageCount)
                {
                    await Task.Delay(durationBetweenPush);
                }
            }

            return await Task.WhenAll(tasks);
        }

        private async Task<PackageResult> TestPackageAsync(int number, int total, TestParameters parameters)
        {
            await Task.Yield();
            try
            {
                var package = parameters.PackagePusher.GeneratePackage(
                    parameters.IdPattern,
                    parameters.VersionPattern);
                _logger.LogInformation(
                    "Pushing package {Number}/{Total} ({Id} {Version})...",
                    number,
                    total,
                    package.Id,
                    package.Version);

                var started = DateTimeOffset.Now;
                var pushStopwatch = Stopwatch.StartNew();
                await parameters.PackagePusher.PushPackageAsync(parameters.ApiKey, package.Bytes);
                pushStopwatch.Stop();
                _logger.LogInformation(
                    "Pushed package {Number}/{Total} ({Id} {Version}). Took {Duration}.",
                    number,
                    total,
                    package.Id,
                    package.Version,
                    pushStopwatch.Elapsed);

                if (number == total)
                {
                    _logger.LogInformation("All packages have been pushed. Waiting until all packages are available.");
                }

                var endpointResults = await _packageChecker.GetEndpointResultsAsync(
                    parameters.EndpointCheckers,
                    package.Id,
                    package.Version);

                var packageResult = new PackageResult(
                    package,
                    started,
                    pushStopwatch.Elapsed,
                    endpointResults);

                parameters.OnPackageResult?.Invoke(packageResult);

                return packageResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "An error occurred while testing package {Number}/{Total}.", number, total);
                throw;
            }
        }
    }
}
