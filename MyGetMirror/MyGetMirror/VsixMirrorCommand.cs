using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;

namespace MyGetMirror
{
    public class VsixMirrorCommand
    {
        private const string PushTaskPrefix = "PUSH-";
        private const string EnumerateTaskName = "ENUMERATE";

        private readonly VsixPackageEnumerator _enumerator;
        private readonly ILogger _logger;
        private readonly int _maxDegreeOfParallelism;
        private readonly VsixPackageMirrorCommand _mirror;
        private readonly int _taskNameWidth;
        private readonly int _pushTaskNameDigitWidth;

        public VsixMirrorCommand(int maxDegreeOfParallelism, VsixPackageEnumerator enumerator, VsixPackageMirrorCommand mirror, ILogger logger)
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
            var packages = new ConcurrentBag<VsixPackage>();

            var isFullyEnumerated = new ManualResetEventSlim(false);

            var populateTask = PopulatePackagesAsync(packages, isFullyEnumerated, token);
            
            var processTasks = Enumerable
                .Range(0, _maxDegreeOfParallelism)
                .Select(i => GetPushTaskName(i))
                .Select(taskName => ProcessPackagesAsync(taskName, packages, isFullyEnumerated, token))
                .ToArray();

            await Task.WhenAll(processTasks.Concat(new[] { populateTask }));
        }

        private async Task PopulatePackagesAsync(ConcurrentBag<VsixPackage> packages, ManualResetEventSlim isFullyEnumerated, CancellationToken token)
        {
            var taskName = GetEnumerateTaskName();
            var result = await _enumerator.EnumerateAsync(token);

            foreach (var package in result)
            {
                packages.Add(package);
            }

            _logger.LogInformationSummary($"[ {taskName} ] A page of {result.Count} VSIX package(s) has been fetched.");

            isFullyEnumerated.Set();

            _logger.LogInformationSummary($"[ {taskName} ] The VSIX packages have been fully enumerated. {result.Count} package(s) in total.");
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

        private async Task ProcessPackagesAsync(string taskName, ConcurrentBag<VsixPackage> packages, ManualResetEventSlim isFullyEnumerated, CancellationToken token)
        {
            while (true)
            {
                VsixPackage package;

                if (packages.TryTake(out package))
                {
                    var stopwatch = Stopwatch.StartNew();
                    var pushed = await _mirror.MirrorAsync(package.Id, package.Version, token);

                    if (pushed)
                    {
                        _logger.LogInformationSummary($"[ {taskName} ] VSIX package {package} took {stopwatch.Elapsed.TotalSeconds:0.00} seconds to publish.");
                    }
                    else
                    {
                        _logger.LogInformationSummary($"[ {taskName} ] VSIX package {package} took {stopwatch.Elapsed.TotalSeconds:0.00} seconds to detect no push was necessary.");
                    }

                    _logger.LogInformationSummary($"[ {taskName} ] {packages.Count} VSIX package(s) remain in the queue.");
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
