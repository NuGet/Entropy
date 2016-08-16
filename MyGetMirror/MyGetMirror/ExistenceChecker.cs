using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace MyGetMirror
{
    public class NuGetPackageExistenceChecker
    {
        private readonly ILogger _logger;
        private readonly MetadataResource _metadataResource;
        private readonly INuGetSymbolsPackageDownloader _symbolsPackageDownloader;

        public NuGetPackageExistenceChecker(MetadataResource metadataResource, INuGetSymbolsPackageDownloader symbolsPackageDownloader, ILogger logger)
        {
            _metadataResource = metadataResource;
            _symbolsPackageDownloader = symbolsPackageDownloader;
            _logger = logger;
        }

        public async Task<bool> PackageExistsAsync(PackageIdentity identity, CancellationToken token)
        {
            return await _metadataResource.Exists(identity, _logger, token);
        }

        public async Task<bool> SymbolsPackageExistsAsync(PackageIdentity identity, CancellationToken token)
        {
            return await _symbolsPackageDownloader.IsAvailableAsync(identity, token);
        }
    }
}
