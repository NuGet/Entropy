using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageDownloader
{
    class Program
    {
        private const string RootMarker = "discover-packages.ps1";

        static async Task<int> Main(string[] args)
        {
            if (!TryFindRoot(out var rootDir))
            {
                return 1;
            }

            // Read IDs from *-ids.txt
            var ids = Directory
                .EnumerateFiles(rootDir, "*-ids.txt")
                .SelectMany(filePath => File.ReadAllLines(filePath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            Console.WriteLine($"Found {ids.Count} package IDs in the log files.");

            var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

            var idBag = new ConcurrentBag<string>(ids);
            var idVersionBag = new ConcurrentBag<PackageIdentity>();

            // Download all of the versions of every package ID
            var workers = Enumerable
                .Range(0, 32)
                .Select(async i =>
                {
                    while (idBag.Count > 0 || idVersionBag.Count > 0)
                    {
                        while (idBag.TryTake(out var id))
                        {
                            using (var cacheContext = new SourceCacheContext())
                            {
                                Console.WriteLine($"[{i,2}] Getting version list for {id}...");
                                var versions = await resource.GetAllVersionsAsync(id, cacheContext, NullLogger.Instance, CancellationToken.None);
                                foreach (var version in versions)
                                {
                                    idVersionBag.Add(new PackageIdentity(id, version));
                                }
                            }
                        }

                        while (idVersionBag.TryTake(out var identity))
                        {
                            using (var cacheContext = new SourceCacheContext())
                            {
                                var path = Path.Combine(rootDir, "nupkgs", $"{identity.Id.ToLowerInvariant()}.{identity.Version.ToNormalizedString().ToLowerInvariant()}.nupkg");
                                if (File.Exists(path))
                                {
                                    continue;
                                }
                                Console.WriteLine($"[{i,2}] Downloading {identity.Id} {identity.Version.ToNormalizedString()}...");
                                var downloader = await resource.GetPackageDownloaderAsync(identity, cacheContext, NullLogger.Instance, CancellationToken.None);
                                await downloader.CopyNupkgFileToAsync($"{path}.download", CancellationToken.None);
                                File.Move($"{path}.download", path);
                            }
                        }
                    }
                })
                .ToList();

            await Task.WhenAll(workers);

            return 0;
        }

        static bool TryFindRoot(out string dir)
        {
            dir = Directory.GetCurrentDirectory();
            var tried = new List<string>();
            bool foundRoot;
            do
            {
                tried.Add(dir);
                foundRoot = Directory
                    .EnumerateFiles(dir)
                    .Select(filePath => Path.GetFileName(filePath))
                    .Contains(RootMarker);

                if (!foundRoot)
                {
                    dir = Path.GetDirectoryName(dir);
                }
            }
            while (dir != null && !foundRoot);

            if (!foundRoot)
            {
                Console.WriteLine($"Could not find {RootMarker} in the current or parent directories.");
                Console.WriteLine("Tried:");
                foreach (var path in tried)
                {
                    Console.WriteLine($"  {path}");
                }

                dir = null;
                return false;
            }

            return true;
        }
    }
}
