using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGetReleaseTool.GenerateInsertionChangelogCommand;

namespace NuGetReleaseTool.GenerateRedundantPackageListCommand
{
    public class UnlistRedudantPackagesCommand
    {
        private UnlistRedundantPackagesCommandOptions Options { get; set; }

        public UnlistRedudantPackagesCommand(UnlistRedundantPackagesCommandOptions options)
        {
            Options = options;
        }

        public async Task RunAsync()
        {
            if (!Options.DryRun && string.IsNullOrEmpty(Options.APIKey))
            {
                throw new ArgumentException("When not running a dry run an API key must be provided");
            }
            SourceRepository sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
            var sourceCacheContext = new SourceCacheContext
            {
                NoCache = true,
                RefreshMemoryCache = true
            };

            List<string> packagesToHandle = Constants.CorePackagesList.Concat(Constants.VSPackagesList).Concat(new string[] { Constants.NuGetCommandlinePackageId }).ToList();

            List<PackageIdentity> packagesToUnlist = new();
            foreach (var package in packagesToHandle)
            {
                var (remainingPackages, redundantPackages) = await GetRedundantPackagesAsync(metadataResource, sourceCacheContext, package);
                Console.WriteLine($"Found {redundantPackages.Count} redundant package versions for {package}");
                packagesToUnlist.AddRange(redundantPackages);
            }

            if (Options.DryRun)
            {
                Console.WriteLine($"{packagesToUnlist.Count} redundant packages found:");
                foreach (var package in packagesToUnlist)
                {
                    Console.WriteLine($"{package.Id} {package.Version.ToNormalizedString()}");
                }
            }
            else
            {
                Console.WriteLine($"Unlisting {packagesToUnlist.Count} redundant packages.");
                var packageUpdateResource = await sourceRepository.GetResourceAsync<PackageUpdateResource>();
                foreach (var package in packagesToUnlist)
                {
                    Console.WriteLine($"Unlisting package {package}");
                    await packageUpdateResource.Delete(package.Id, package.Version.ToNormalizedString(), apiKey => Options.APIKey, nonInteractive => true, noServiceEndpoint: false, NullLogger.Instance);
                }
            }
        }

        private static async Task<(List<PackageIdentity>, List<PackageIdentity>)> GetRedundantPackagesAsync(PackageMetadataResource metadataResource, SourceCacheContext sourceCacheContext, string packageId)
        {
            List<PackageIdentity> redundantPackages = new();
            List<PackageIdentity> remainingPackages = new();

            var packages = await metadataResource.GetMetadataAsync(packageId, includePrerelease: true, includeUnlisted: false, sourceCacheContext, NullLogger.Instance, CancellationToken.None);
            var packageVersions = packages.Select(e => e.Identity.Version).ToList();
            var groupedPackageVersions = packageVersions.GroupBy(e => new Version(e.Major, e.Minor));
            var maxVersion = packageVersions.Max();

            foreach (var group in groupedPackageVersions)
            {

                var packageGroupVersions = group.ToList();
                if (packageGroupVersions.Count > 1)
                {
                    var maxGroupVersion = packageGroupVersions.Max();
                    var maxPrereleaseGroupVersion = packageGroupVersions.Where(e => e.IsPrerelease).Max();
                    var maxStableVersion = packageGroupVersions.Where(e => !e.IsPrerelease).Max();
                    foreach (var version in packageGroupVersions)
                    {
                        if (maxStableVersion == version // Keep latest stable in range.
                            || (maxGroupVersion == version && maxStableVersion != null && version > maxStableVersion) // Keep latest in range, only if there are any stable version in the range and this version is bigger than that.
                            || version == maxVersion // Keep if absolute latest prerelease 
                            )
                        {
                            remainingPackages.Add(new PackageIdentity(packageId, version));
                        }
                        else
                        {
                            redundantPackages.Add(new PackageIdentity(packageId, version));
                        }
                    }
                }
            }
            return (remainingPackages, redundantPackages);
        }
    }
}
