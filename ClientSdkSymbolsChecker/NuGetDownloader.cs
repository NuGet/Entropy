using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace ClientSdkSymbolsChecker
{
    internal class NuGetDownloader
    {
        private SourceRepository NuGetOrg { get; }
        private FindPackageByIdResource FindPackageByIdResource { get; }
        private SourceCacheContext SourceCacheContext { get; }

        public NuGetDownloader(
            SourceRepository nugetOrg,
            FindPackageByIdResource findPackageByIdResource)
        {
            NuGetOrg = nugetOrg ?? throw new ArgumentNullException(nameof(nugetOrg));
            FindPackageByIdResource = findPackageByIdResource ?? throw new ArgumentNullException(nameof(findPackageByIdResource));

            SourceCacheContext = new();
        }

        public static async Task<NuGetDownloader> CreateAsync(CancellationToken cancellationToken)
        {
            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json", "nuget.org");
            var nuGetOrg = Repository.Factory.GetCoreV3(packageSource);
            var findPackageByIdResource = await nuGetOrg.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

            return new NuGetDownloader(nuGetOrg, findPackageByIdResource);
        }

        public async Task<IReadOnlyList<NuGetVersion>> GetAllVersionsAsync(string packageId, CancellationToken cancellationToken)
        {
            // I hate that this API returns IEnumerable<T>.
            var enumerable = await this.FindPackageByIdResource.GetAllVersionsAsync(packageId, SourceCacheContext, NullLogger.Instance, cancellationToken);
            var allVersions = enumerable.ToList().AsReadOnly();
            return allVersions;
        }
    }
}
