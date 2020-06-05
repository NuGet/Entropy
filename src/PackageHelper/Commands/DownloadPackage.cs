using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace PackageHelper.Commands
{
    static class DownloadPackage
    {
        public static Command GetCommand()
        {
            var command = new Command("download-package")
            {
                Description = "Download a specific package version from a source",
            };

            command.Add(new Argument<string>("id")
            {
                Description = "The package ID to download"
            });

            command.Add(new Argument<string>("version")
            {
                Description = "The package version to download"
            });

            command.Add(new Option<string>(
                "--source",
                getDefaultValue: () => DownloadAllVersions.NuGetOrg)
            {
                Description = "The package source to download from"
            });

            command.Handler = CommandHandler.Create<string, string, string>(ExecuteAsync);

            return command;
        }

        static async Task<int> ExecuteAsync(string id, string version, string source)
        {
            if (!Helper.TryFindRoot(out var rootDir))
            {
                return 1;
            }

            var nupkgDir = Path.Combine(rootDir, "out", "nupkgs");

            var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

            var lowerId = id.ToLowerInvariant();
            var lowerVersion = version.ToLowerInvariant();

            var path = Path.Combine(nupkgDir, id, version, $"{lowerId}.{lowerVersion}.nupkg");
            if (File.Exists(path))
            {
                Console.WriteLine($"Package {id} {version} is already downloaded.");
                return 1;
            }

            Console.WriteLine($"Downloading {id} {version}...");
            using var cacheContext = Helper.GetCacheContext();
            var downloader = await resource.GetPackageDownloaderAsync(
                new PackageIdentity(id, NuGetVersion.Parse(version)),
                cacheContext,
                NullLogger.Instance,
                CancellationToken.None);

            if (downloader == null)
            {
                Console.WriteLine($"Package {id} {version} does not exist on source {source}.");
                return 1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (await downloader.CopyNupkgFileToAsync($"{path}.download", CancellationToken.None))
            {
                File.Move($"{path}.download", path);
            }

            return 0;
        }
    }
}
