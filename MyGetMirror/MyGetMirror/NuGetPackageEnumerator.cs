using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace MyGetMirror
{
    public class NuGetPackageEnumerator
    {
        private readonly ILogger _logger;
        private readonly PackageSearchResource _packageSearchResource;

        public NuGetPackageEnumerator(PackageSearchResource packageSearchResource, ILogger logger)
        {
            _packageSearchResource = packageSearchResource;
            _logger = logger;
        }

        public PackageEnumeratorResult GetInitialResult()
        {
            return new PackageEnumeratorResult
            {
                HasMoreResults = true,
                ContinuationToken = new PackageEnumeratorContinuationToken
                {
                    SearchTerm = string.Empty,
                    SearchFilters = new SearchFilter(
                        supportedFrameworks: Enumerable.Empty<string>(),
                        includePrerelease: true,
                        includeDelisted: true,
                        packageTypes: Enumerable.Empty<string>()),
                    Skip = 0,
                    Take = 50
                },
                PackageIdentities = new PackageIdentity[0]
            };
        }

        public async Task<PackageEnumeratorResult> GetPageAsync(PackageEnumeratorContinuationToken continuationToken, CancellationToken token)
        {
            var result = await _packageSearchResource.SearchAsync(
                continuationToken.SearchTerm,
                continuationToken.SearchFilters,
                continuationToken.Skip,
                continuationToken.Take,
                _logger,
                token);

            var allMetadata = result.ToList();
            var allVersions = allMetadata
                .Select(x => new { x.Identity, AllVersionsTask = x.GetVersionsAsync() })
                .ToList();

            await Task.WhenAll(allVersions.Select(x => x.AllVersionsTask));

            var packageIdentities = allVersions
                .Select(x => new { Id = x.Identity.Id, Versions = x.AllVersionsTask.Result })
                .SelectMany(x => x.Versions.Select(v => new PackageIdentity(x.Id, v.Version)))
                .ToList();

            var hasMoreResults = allMetadata.Count >= continuationToken.Take;
            var nextSkip = continuationToken.Skip + continuationToken.Take;
            var nextContinuationToken = new PackageEnumeratorContinuationToken
            {
                SearchTerm = continuationToken.SearchTerm,
                SearchFilters = continuationToken.SearchFilters,
                Skip = nextSkip,
                Take = continuationToken.Take
            };

            return new PackageEnumeratorResult
            {
                HasMoreResults = hasMoreResults,
                PackageIdentities = packageIdentities,
                ContinuationToken = nextContinuationToken
            };
        }
    }
}
