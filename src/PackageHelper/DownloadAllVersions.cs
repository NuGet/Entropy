using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageHelper
{
    static class DownloadAllVersions
    {
        public const string Name = "download-all-versions";

        public static async Task<int> ExecuteAsync(string[] args)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            var nupkgDir = Path.Combine(rootDir, "out", "nupkgs");
            var ids = Directory
                .EnumerateDirectories(nupkgDir)
                .Select(x => Path.GetFileName(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            Console.WriteLine($"Found {ids.Count} package IDs in the log files.");

            var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

            var idBag = new ConcurrentQueue<string>(ids);
            var idVersionBag = new ConcurrentQueue<PackageIdentity>();

            // Download all of the versions of every package ID
            var workers = Enumerable
                .Range(0, 32)
                .Select(async i =>
                {
                    while (idBag.Count > 0 || idVersionBag.Count > 0)
                    {
                        while (idBag.TryDequeue(out var id))
                        {
                            using var cacheContext = Helper.GetCacheContext();
                            Console.WriteLine($"[{i,2}] Getting version list for {id}...");
                            var versions = (await resource.GetAllVersionsAsync(id, cacheContext, NullLogger.Instance, CancellationToken.None)).ToList();
                            foreach (var version in versions)
                            {
                                idVersionBag.Enqueue(new PackageIdentity(id, version));
                            }
                        }

                        while (idVersionBag.TryDequeue(out var identity))
                        {
                            var lowerId = identity.Id.ToLowerInvariant();
                            var lowerVersion = identity.Version.ToNormalizedString().ToLowerInvariant();

                            var path = Path.Combine(
                                nupkgDir,
                                lowerId,
                                lowerVersion,
                                $"{lowerId}.{lowerVersion}.nupkg");
                            if (File.Exists(path))
                            {
                                continue;
                            }

                            Console.WriteLine($"[{i,2}] [{idVersionBag.Count,6}] Downloading {identity.Id} {identity.Version.ToNormalizedString()}...");
                            using var cacheContext = Helper.GetCacheContext();
                            var downloader = await resource.GetPackageDownloaderAsync(identity, cacheContext, NullLogger.Instance, CancellationToken.None);
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            if (await downloader.CopyNupkgFileToAsync($"{path}.download", CancellationToken.None))
                            {
                                File.Move($"{path}.download", path);
                            }

                        }
                    }
                })
                .ToList();

            await Task.WhenAll(workers);

            return 0;
        }
    }
}
