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
            var pushedVersions = new Dictionary<string, HashSet<NuGetVersion>>(StringComparer.OrdinalIgnoreCase);
            var consoleLogger = new ConsoleLogger();

            foreach (var nupkgPath in Directory.EnumerateFiles(nupkgDir, "*.nupkg", SearchOption.AllDirectories))
            {
                using var reader = new PackageArchiveReader(nupkgPath);
                var identity = reader.GetIdentity();

                if (!pushedVersions.TryGetValue(identity.Id, out var versions))
                {
                    Console.WriteLine($"Checking pushed versions of {identity.Id}...");
                    using var cacheContext = new SourceCacheContext();
                    versions = (await findPackageById.GetAllVersionsAsync(identity.Id, cacheContext, NullLogger.Instance, CancellationToken.None)).ToHashSet();
                    pushedVersions.Add(identity.Id, versions);
                }

                if (versions.Contains(identity.Version))
                {
                    continue;
                }

                Console.WriteLine($"Pushing {identity.Id} {identity.Version.ToNormalizedString()}...");
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
                    log: consoleLogger);
            }

            return 0;
        }
    }
}
