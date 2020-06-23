using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace PackageHelper.Commands
{
    class Push
    {
        public static Command GetCommand()
        {
            var command = new Command("push")
            {
                Description = "Push all downloaded packages to the provided source",
            };

            command.Add(new Argument<string>("push-source")
            {
                Description = "Package source to push to",
            });
            command.Add(new Option<string>("--list-source")
            {
                Description = "Package source to list versions from, to avoid pushing duplicates",
            });
            command.Add(new Option<string>("--api-key")
            {
                Description = "API key to use when pushing packages",
            });
            command.Add(new Option<int>("--max-concurrency", getDefaultValue: () => 8)
            {
                Description = "Maximum number of parallel pushes",
            });
            command.Add(new Option<int>("--max-id-concurrency", getDefaultValue: () => 8)
            {
                Description = "Maximum number of parallel pushes to a single ID",
            });

            command.Handler = CommandHandler.Create<string, string, string, int, int>(ExecuteAsync);

            return command;
        }

        static async Task<int> ExecuteAsync(string pushSource, string listSource, string apiKey, int maxConcurrency, int maxIdConcurrency)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            Console.WriteLine($"Using push package source: {pushSource}");

            if (string.IsNullOrWhiteSpace(listSource))
            {
                listSource = pushSource;
            }

            Console.WriteLine($"Using list package source: {listSource}");

            var nupkgDir = Path.Combine(rootDir, "out", "nupkgs");
            Console.WriteLine($"Scanning {nupkgDir} for NuGet packages...");

            var packageUpdate = await Repository.Factory.GetCoreV3(pushSource).GetResourceAsync<PackageUpdateResource>();
            var findPackageById = await Repository.Factory.GetCoreV3(listSource).GetResourceAsync<FindPackageByIdResource>();
            var pushedVersionsLock = new object();
            var idToSemaphore = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
            var pushedVersions = new Dictionary<string, Task<HashSet<NuGetVersion>>>(StringComparer.OrdinalIgnoreCase);
            var work = new ConcurrentBag<string>(Directory
                .EnumerateFiles(nupkgDir, "*.nupkg", SearchOption.AllDirectories)
                .OrderBy(x => Guid.NewGuid()));
            var consoleLock = new object();

            var workers = Enumerable
                .Range(0, maxConcurrency)
                .Select(async i =>
                {
                    while (work.TryTake(out var nupkgPath))
                    {
                        using var reader = new PackageArchiveReader(nupkgPath);
                        var identity = reader.GetIdentity();
                        Console.WriteLine($"[{work.Count} remaining] Processing {identity.Id} {identity.Version}...");
                        await PushAsync(
                            findPackageById,
                            packageUpdate,
                            apiKey,
                            identity,
                            nupkgPath,
                            pushedVersionsLock,
                            id => idToSemaphore.GetOrAdd(id, _ => new SemaphoreSlim(maxIdConcurrency)),
                            pushedVersions,
                            consoleLock,
                            allowRetry: true);
                    }
                })
                .ToList();

            await Task.WhenAll(workers);

            return 0;
        }

        static async Task PushAsync(
            FindPackageByIdResource findPackageById,
            PackageUpdateResource packageUpdate,
            string apiKey,
            PackageIdentity identity,
            string nupkgPath,
            object pushedVersionsLock,
            Func<string, SemaphoreSlim> getIdSemaphore,
            Dictionary<string, Task<HashSet<NuGetVersion>>> pushedVersions,
            object consoleLock,
            bool allowRetry)
        {
            // Get the list of existing versions.
            Task<HashSet<NuGetVersion>> versionsTask;
            lock (pushedVersionsLock)
            {
                if (!pushedVersions.TryGetValue(identity.Id, out versionsTask))
                {
                    versionsTask = GetVersionsAsync(findPackageById, identity.Id, consoleLock);
                    pushedVersions.Add(identity.Id, versionsTask);
                }
            }

            var versions = await versionsTask;

            lock (pushedVersionsLock)
            {
                if (versions.Contains(identity.Version))
                {
                    return;
                }
            }

            Console.WriteLine($"  Pushing {identity.Id} {identity.Version.ToNormalizedString()}...");
            try
            {
                var idSemaphore = getIdSemaphore(identity.Id);
                await idSemaphore.WaitAsync();
                try
                {
                    await packageUpdate.Push(
                       nupkgPath,
                       symbolSource: string.Empty,
                       timeoutInSecond: 300,
                       disableBuffering: false,
                       getApiKey: _ => apiKey,
                       getSymbolApiKey: null,
                       noServiceEndpoint: false,
                       skipDuplicate: false,
                       symbolPackageUpdateResource: null,
                       log: NullLogger.Instance); // new ConsoleLogger());
                }
                finally
                {
                    idSemaphore.Release();
                }

                lock (pushedVersionsLock)
                {
                    versions.Add(identity.Version);
                }
            }
            catch (HttpRequestException ex) when (ex.Message.StartsWith("Response status code does not indicate success: 409 ") && allowRetry)
            {
                await PushAsync(
                    findPackageById,
                    packageUpdate,
                    apiKey,
                    identity,
                    nupkgPath,
                    pushedVersionsLock,
                    getIdSemaphore,
                    pushedVersions,
                    consoleLock,
                    allowRetry: false);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine($"  Push of {identity.Id} {identity.Version.ToNormalizedString()} ({new FileInfo(nupkgPath).Length} bytes) failed with exception:");
                    Console.WriteLine(ex);
                }
                return;
            }
        }

        static async Task<HashSet<NuGetVersion>> GetVersionsAsync(FindPackageByIdResource findPackageById, string id, object consoleLock)
        {
            lock (consoleLock)
            {
                Console.WriteLine($"  Checking pushed versions of {id}...");
            }

            using var cacheContext = Helper.GetCacheContext();
            return (await findPackageById.GetAllVersionsAsync(
                id,
                cacheContext,
                NullLogger.Instance, // new ConsoleLogger(),
                CancellationToken.None)).ToHashSet();
        }
    }
}
