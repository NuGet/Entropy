using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace MyGetMirror
{
    public class FeedMirrorCommand
    {
        public async Task ExecuteAsync(FeedMirrorRequest request, ILogger logger, CancellationToken token)
        {
            var settings = new InMemorySettings();
            SettingsUtility.SetConfigValue(
                settings,
                "globalPackagesFolder",
                request.PackagesDirectory);

            // Set up the source logic.
            var sourceRepository = Repository.Factory.GetCoreV3(request.Source);
            var sourceDownloaderResource = await sourceRepository.GetResourceAsync<DownloadResource>(token);
            var sourceHttpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);
            var sourceHttpSource = sourceHttpSourceResource.HttpSource;
            var sourceSymbolsPackageDownloader = new MyGetNuGetSymbolsPackageDownloader(request.Source, sourceHttpSource, logger);

            // Set up the destination logic.
            var destinationRepository = Repository.Factory.GetCoreV3(request.Destination);
            var destinationHttpSourceResource = await destinationRepository.GetResourceAsync<HttpSourceResource>(token);
            var destinationHttpSource = destinationHttpSourceResource.HttpSource;
            var destinationMetadataResource = await destinationRepository.GetResourceAsync<MetadataResource>(token);
            var destinationSymbolsPackageDownloader = new MyGetNuGetSymbolsPackageDownloader(request.Destination, destinationHttpSource, logger);
            var destinationExistenceChecker = new NuGetPackageExistenceChecker(destinationMetadataResource, destinationSymbolsPackageDownloader, logger);
            var destinationNuGetPackagePusher = new NuGetPackagePusher(request.PushDestination, request.DestinationApiKey, destinationHttpSource, logger);

            // Set up the mirror implementations.
            var nuGetPackageMirror = new NuGetPackageMirrorCommand(
                request.OverwriteExisting,
                request.IncludeNuGetSymbols,
                sourceDownloaderResource,
                sourceSymbolsPackageDownloader,
                destinationExistenceChecker,
                destinationNuGetPackagePusher,
                settings,
                logger);

            var sourcePackageSearchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>(token);
            var nuGetPackageEnumerator = new NuGetPackageEnumerator(
                sourcePackageSearchResource,
                logger);

            var nuGetMirror = new NuGetMirrorCommand(
                request.MaxDegreeOfParallelism,
                nuGetPackageEnumerator,
                nuGetPackageMirror,
                logger);

            // Execute.
            if (request.IncludeNuGet)
            {
                await nuGetMirror.Execute(token);
            }
        }
    }
}
