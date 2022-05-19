using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.Versioning;

namespace ClientSdkSymbolsChecker
{
    internal class NuGetDownloader
    {
        private SourceRepository NuGetOrg { get; }
        private FindPackageByIdResource FindPackageByIdResource { get; }
        private DownloadResource DownloadResource { get; }
        private SourceCacheContext SourceCacheContext { get; }

        private NuGetDownloader(
            SourceRepository nugetOrg,
            FindPackageByIdResource findPackageByIdResource,
            DownloadResource downloadResource)
        {
            NuGetOrg = nugetOrg ?? throw new ArgumentNullException(nameof(nugetOrg));
            FindPackageByIdResource = findPackageByIdResource ?? throw new ArgumentNullException(nameof(findPackageByIdResource));
            DownloadResource = downloadResource ?? throw new ArgumentNullException(nameof(downloadResource));

            SourceCacheContext = new();
        }

        public static async Task<NuGetDownloader> CreateAsync(CancellationToken cancellationToken)
        {
            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json", "nuget.org");
            var nuGetOrg = Repository.Factory.GetCoreV3(packageSource);
            var findPackageByIdResource = await nuGetOrg.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            var downloadResource = await nuGetOrg.GetResourceAsync<DownloadResource>(cancellationToken);

            return new NuGetDownloader(nuGetOrg, findPackageByIdResource, downloadResource);
        }

        public async Task<(string packageId, IReadOnlyList<NuGetVersion>)> GetAllVersionsAsync(string packageId, CancellationToken cancellationToken)
        {
            // I hate that this API returns IEnumerable<T>.
            var enumerable = await FindPackageByIdResource.GetAllVersionsAsync(packageId, SourceCacheContext, NullLogger.Instance, cancellationToken);
            var allVersions = enumerable.ToList().AsReadOnly();
            return (packageId, allVersions);
        }

        public async Task DownloadPackageAsync(PackageIdentity packageIdentity, NuGetv3LocalRepository destination, CancellationToken cancellationToken)
        {
            var packageDownloadContext = new PackageDownloadContext(SourceCacheContext);
            var result = await DownloadResource.GetDownloadResourceResultAsync(packageIdentity, packageDownloadContext, destination.RepositoryRoot, NullLogger.Instance, cancellationToken);
            if (result?.Status != DownloadResourceResultStatus.Available)
            {
                throw new FatalProtocolException("Unable to download package " + packageIdentity);
            }
        }
    }
}
