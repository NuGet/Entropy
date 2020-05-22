using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PackageHelper.Commands
{
    class Push
    {
        public const string Name = "push";

        public static async Task<int> ExecuteAsync(string[] args)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            string pushPackageSource;
            if (args.Length == 0)
            {
                Console.WriteLine($"The {Name} command requires a package source as the first argument.");
                return 1;
            }

            pushPackageSource = args[0];
            Console.WriteLine($"Using push package source: {pushPackageSource}");

            string listPackageSource;
            if (args.Length > 1)
            {
                listPackageSource = args[1];
            }
            else
            {
                listPackageSource = pushPackageSource;
            }

            Console.WriteLine($"Using list package source: {listPackageSource}");

            string apiKey = null;
            if (args.Length > 2)
            {
                Console.WriteLine("Using an API passed as an argument.");
                apiKey = args[2];
            }

            var nupkgDir = Path.Combine(rootDir, "out", "nupkgs");
            Console.WriteLine($"Scanning {nupkgDir} for NuGet packages...");

            var packageUpdate = await Repository.Factory.GetCoreV3(pushPackageSource).GetResourceAsync<PackageUpdateResource>();
            var findPackageById = await Repository.Factory.GetCoreV3(listPackageSource).GetResourceAsync<FindPackageByIdResource>();
            var pushedVersionsLock = new object();
            var pushedVersions = new Dictionary<string, Task<HashSet<NuGetVersion>>>(StringComparer.OrdinalIgnoreCase);
            var work = new ConcurrentQueue<string>(Directory.EnumerateFiles(nupkgDir, "*.nupkg", SearchOption.AllDirectories));
            var consoleLock = new object();

            var workers = Enumerable
                .Range(0, 8)
                .Select(async i =>
                {
                    while (work.TryDequeue(out var nupkgPath))
                    {
                        await PushAsync(findPackageById, packageUpdate, apiKey, nupkgPath, pushedVersionsLock, pushedVersions, consoleLock, allowRetry: true);
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
            string nupkgPath,
            object pushedVersionsLock,
            Dictionary<string, Task<HashSet<NuGetVersion>>> pushedVersions,
            object consoleLock,
            bool allowRetry)
        {
            using var reader = new PackageArchiveReader(nupkgPath);
            var identity = reader.GetIdentity();

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

            Console.WriteLine($"Pushing {identity.Id} {identity.Version.ToNormalizedString()}...");
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

                lock (pushedVersionsLock)
                {
                    versions.Add(identity.Version);
                }
            }
            catch (HttpRequestException ex) when (ex.Message.StartsWith("Response status code does not indicate success: 409 ") && allowRetry)
            {
                await PushAsync(findPackageById, packageUpdate, apiKey, nupkgPath, pushedVersionsLock, pushedVersions, consoleLock, allowRetry: false);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine($"Push of {identity.Id} {identity.Version.ToNormalizedString()} ({new FileInfo(nupkgPath).Length} bytes) failed with exception:");
                    Console.WriteLine(ex);
                }
                return;
            }
        }

        static async Task<HashSet<NuGetVersion>> GetVersionsAsync(FindPackageByIdResource findPackageById, string id, object consoleLock)
        {
            lock (consoleLock)
            {
                Console.WriteLine($"Checking pushed versions of {id}...");
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
