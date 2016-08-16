using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;

namespace MyGetMirror
{
    public class NuGetMirrorCommand
    {
        private const string PushTaskPrefix = "PUSH-";
        private const string EnumerateTaskName = "ENUMERATE";

        private readonly NuGetPackageEnumerator _enumerator;
        private readonly ILogger _logger;
        private readonly int _maxDegreeOfParallelism;
        private readonly NuGetPackageMirrorCommand _mirror;
        private readonly int _taskNameWidth;
        private readonly int _pushTaskNameDigitWidth;

        public NuGetMirrorCommand(int maxDegreeOfParallelism, NuGetPackageEnumerator enumerator, NuGetPackageMirrorCommand mirror, ILogger logger)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _enumerator = enumerator;
            _mirror = mirror;
            _logger = logger;

            _pushTaskNameDigitWidth = (int)Math.Ceiling(Math.Log10(_maxDegreeOfParallelism));
            var minimumNameWidth = PushTaskPrefix.Length + _pushTaskNameDigitWidth;
            _taskNameWidth = Math.Max(minimumNameWidth, EnumerateTaskName.Length);
        }

        public async Task Execute(CancellationToken token)
        {
            var identities = new ConcurrentBag<PackageIdentity>();

            var isFullyEnumerated = new ManualResetEventSlim(false);

            var populateTask = PopulatePackageIdentitiesAsync(identities, isFullyEnumerated, token);
            
            var processTasks = Enumerable
                .Range(0, _maxDegreeOfParallelism)
                .Select(i => GetPushTaskName(i))
                .Select(taskName => ProcessPackageIdentitiesAsync(taskName, identities, isFullyEnumerated, token))
                .ToArray();

            await Task.WhenAll(processTasks.Concat(new[] { populateTask }));
        }

        private async Task PopulatePackageIdentitiesAsync(ConcurrentBag<PackageIdentity> packageIdentities, ManualResetEventSlim isFullyEnumerated, CancellationToken token)
        {
            var taskName = GetEnumerateTaskName();
            var result = _enumerator.GetInitialResult();
            var total = 0;

            while (result.HasMoreResults)
            {
                result = await _enumerator.GetPageAsync(result.ContinuationToken, token);
                total += result.PackageIdentities.Count;

                _logger.LogInformationSummary($"[ {taskName} ] A page of {result.PackageIdentities.Count} NuGet package(s) has been fetched.");

                foreach (var identity in result.PackageIdentities)
                {
                    packageIdentities.Add(identity);
                }
            }

            isFullyEnumerated.Set();
            _logger.LogInformationSummary($"[ {taskName} ] The NuGet packages have been fully enumerated. {total} package(s) in total.");
        }

        private string GetEnumerateTaskName()
        {
            var taskName = EnumerateTaskName.PadLeft(_taskNameWidth, ' ');

            return taskName;
        }

        private string GetPushTaskName(int taskNumber)
        {
            var paddedTaskNumber = taskNumber
                .ToString()
                .PadLeft(_pushTaskNameDigitWidth, '0');

            var pushTaskName = PushTaskPrefix + paddedTaskNumber;

            return pushTaskName.PadLeft(_taskNameWidth, ' ');
        }

        private async Task ProcessPackageIdentitiesAsync(string taskName, ConcurrentBag<PackageIdentity> packageIdentities, ManualResetEventSlim isFullyEnumerated, CancellationToken token)
        {
            while (true)
            {
                PackageIdentity identity;

                if (packageIdentities.TryTake(out identity))
                {
                    var stopwatch = Stopwatch.StartNew();
                    var pushed = await _mirror.MirrorAsync(identity, token);

                    if (pushed)
                    {
                        _logger.LogInformationSummary($"[ {taskName} ] NuGet package {identity} took {stopwatch.Elapsed.TotalSeconds:0.00} seconds to publish.");
                    }
                    else
                    {
                        _logger.LogInformationSummary($"[ {taskName} ] NuGet package {identity} took {stopwatch.Elapsed.TotalSeconds:0.00} seconds to detect no push was necessary.");
                    }

                    _logger.LogInformationSummary($"[ {taskName} ] {packageIdentities.Count} NuGet package(s) remain in the queue.");
                }
                else
                {
                    if (isFullyEnumerated.IsSet)
                    {
                        // The concurrent bag is empty and we are done enumerating, so the task
                        // can safely terminate.
                        break;
                    }
                    else
                    {
                        // The enumeration is not done yet, so wait a little bit for more work to
                        // become available.
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                    }
                }
            }
        }
    }
}
