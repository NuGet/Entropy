using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace MyGetMirror
{
    public class NuGetPackageMirrorCommand
    {
        private readonly NuGetPackageExistenceChecker _existenceChecker;
        private readonly bool _includeSymbols;
        private readonly ILogger _logger;
        private readonly bool _overwriteExisting;
        private readonly NuGetPackagePusher _packagePusher;
        private readonly ISettings _settings;
        private readonly DownloadResource _sourceDownloadResource;
        private readonly INuGetSymbolsPackageDownloader _symbolsPackageDownloader;

        public NuGetPackageMirrorCommand(
            bool overwriteExisting,
            bool includeSymbols,
            DownloadResource sourceDownloadResource,
            INuGetSymbolsPackageDownloader symbolsPackageDownloader,
            NuGetPackageExistenceChecker existenceChecker,
            NuGetPackagePusher packagePusher,
            ISettings settings,
            ILogger logger)
        {
            _overwriteExisting = overwriteExisting;
            _includeSymbols = includeSymbols;
            _sourceDownloadResource = sourceDownloadResource;
            _symbolsPackageDownloader = symbolsPackageDownloader;
            _existenceChecker = existenceChecker;
            _packagePusher = packagePusher;
            _settings = settings;
            _logger = logger;
        }

        public async Task<bool> MirrorAsync(PackageIdentity identity, CancellationToken token)
        {
            // Publish the package itself.
            var publishPackage = true;

            if (!_overwriteExisting)
            {
                publishPackage = !(await _existenceChecker.PackageExistsAsync(identity, token));
            }

            if (publishPackage)
            {
                var downloadResult = await _sourceDownloadResource.GetDownloadResourceResultAsync(
                    identity,
                    _settings,
                    _logger,
                    token);

                using (downloadResult)
                {
                    if (downloadResult.Status != DownloadResourceResultStatus.Available)
                    {
                        throw new InvalidOperationException($"The NuGet package '{identity}' is not available on the source.");
                    }

                    await _packagePusher.PushAsync(downloadResult.PackageStream, token);
                }
            }

            // Publish the symbols package.
            var publishSymbolsPackage = _includeSymbols;

            if (!_overwriteExisting)
            {
                publishSymbolsPackage = !(await _existenceChecker.SymbolsPackageExistsAsync(identity, token));
            }

            if (publishSymbolsPackage)
            {
                publishSymbolsPackage = await _symbolsPackageDownloader.ProcessAsync(
                    identity,
                    async streamResult =>
                    {
                        if (!streamResult.IsAvailable)
                        {
                            // The package has no symbols package.
                            return false;
                        }

                        await _packagePusher.PushAsync(streamResult.Stream, token);

                        return true;
                    },
                    token);
            }

            return publishPackage || publishSymbolsPackage;
        }
    }
}
