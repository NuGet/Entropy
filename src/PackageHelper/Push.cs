using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PackageHelper
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

            if (args.Length == 0)
            {
                Console.WriteLine($"The {Name} command requires a package source as the argument.");
                return 1;
            }

            var nupkgDir = Path.Combine(rootDir, "out", "nupkgs");
            Console.WriteLine($"Scanning {nupkgDir} for NuGet packages...");

            var sourceRepository = Repository.Factory.GetCoreV3(args[0]);
            var findPackageById = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();
            var packageUpdate = await sourceRepository.GetResourceAsync<PackageUpdateResource>();
            var pushedVersionsLock = new SemaphoreSlim(1);
            var pushedVersions = new Dictionary<string, HashSet<NuGetVersion>>(StringComparer.OrdinalIgnoreCase);
            var work = new ConcurrentQueue<string>(Directory.EnumerateFiles(nupkgDir, "*.nupkg", SearchOption.AllDirectories));

            var workers = Enumerable
                .Range(0, 8)
                .Select(async i =>
                {
                    while (work.TryDequeue(out var nupkgPath))
                    {
                        await PushAsync(findPackageById, packageUpdate, nupkgPath, pushedVersionsLock, pushedVersions);
                    }
                })
                .ToList();

            await Task.WhenAll(workers);

            return 0;
        }

        static async Task PushAsync(
            FindPackageByIdResource findPackageById,
            PackageUpdateResource packageUpdate,
            string nupkgPath,
            SemaphoreSlim pushedVersionsLock,
            Dictionary<string, HashSet<NuGetVersion>> pushedVersions)
        {
            using var reader = new PackageArchiveReader(nupkgPath);
            var identity = reader.GetIdentity();

            await pushedVersionsLock.WaitAsync();
            HashSet<NuGetVersion> versions;
            try
            {
                if (!pushedVersions.TryGetValue(identity.Id, out versions))
                {
                    Console.WriteLine($"Checking pushed versions of {identity.Id}...");
                    using var cacheContext = new SourceCacheContext();
                    versions = (await findPackageById.GetAllVersionsAsync(identity.Id, cacheContext, NullLogger.Instance, CancellationToken.None)).ToHashSet();
                    pushedVersions.Add(identity.Id, versions);
                }

                if (versions.Count >= 100)
                {
                    Console.WriteLine($"Push of {identity.Id} {identity.Version.ToNormalizedString()} skipped due to too many versions.");
                    return;
                }

                if (versions.Contains(identity.Version))
                {
                    return;
                }

                if (new FileInfo(nupkgPath).Length > 32 * 1024 * 1024)
                {
                    Console.WriteLine($"Push of {identity.Id} {identity.Version.ToNormalizedString()} skipped since it's too large.");
                    return;
                }
            }
            finally
            {
                pushedVersionsLock.Release();
            }

            Console.WriteLine($"Pushing {identity.Id} {identity.Version.ToNormalizedString()}...");
            try
            {
                await packageUpdate.Push(
                   nupkgPath,
                   symbolSource: string.Empty,
                   timeoutInSecond: 300,
                   disableBuffering: false,
                   getApiKey: _ => null,
                   getSymbolApiKey: null,
                   noServiceEndpoint: false,
                   skipDuplicate: true,
                   symbolPackageUpdateResource: null,
                   log: NullLogger.Instance);

                await pushedVersionsLock.WaitAsync();
                try
                {
                    versions.Add(identity.Version);
                }
                finally
                {
                    pushedVersionsLock.Release();
                }
            }
            catch (Exception ex)
            {
                await pushedVersionsLock.WaitAsync();
                try
                {
                    Console.WriteLine($"Push of {identity.Id} {identity.Version.ToNormalizedString()} failed with exception:");
                    Console.WriteLine(ex);
                }
                finally
                {
                    pushedVersionsLock.Release();
                }
                return;
            }
        }
    }
}
