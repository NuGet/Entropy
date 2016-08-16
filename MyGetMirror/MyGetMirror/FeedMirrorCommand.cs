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
            var sourceUrlBuilder = new MyGetUrlBuilder(request.Source);
            var sourceRepository = Repository.Factory.GetCoreV3(request.Source);
            var sourceDownloaderResource = await sourceRepository.GetResourceAsync<DownloadResource>(token);
            var sourceHttpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);
            var sourceHttpSource = sourceHttpSourceResource.HttpSource;
            var sourceSymbolsPackageDownloader = new MyGetNuGetSymbolsPackageDownloader(sourceUrlBuilder, sourceHttpSource, logger);
            var sourcePackageSearchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>(token);
            var sourceVsixPackageDownloader = new MyGetVsixPackageDownloader(sourceUrlBuilder, sourceHttpSource, logger);

            // Set up enumeration logic for the source.
            var nuGetPackageEnumerator = new NuGetPackageEnumerator(sourcePackageSearchResource, logger);
            var vsixPackageEnumerator = new VsixPackageEnumerator(sourceUrlBuilder.GetVsixUrl(), sourceHttpSource, logger);

            // Set up the destination logic.
            var destinationUrlBuilder = new MyGetUrlBuilder(request.Destination);
            var destinationRepository = Repository.Factory.GetCoreV3(request.Destination);
            var destinationHttpSourceResource = await destinationRepository.GetResourceAsync<HttpSourceResource>(token);
            var destinationHttpSource = destinationHttpSourceResource.HttpSource;
            var destinationMetadataResource = await destinationRepository.GetResourceAsync<MetadataResource>(token);
            var destinationSymbolsPackageDownloader = new MyGetNuGetSymbolsPackageDownloader(destinationUrlBuilder, destinationHttpSource, logger);
            var destinationExistenceChecker = new NuGetPackageExistenceChecker(destinationMetadataResource, destinationSymbolsPackageDownloader, logger);
            var destinationVsixPackageDownloader = new MyGetVsixPackageDownloader(destinationUrlBuilder, destinationHttpSource, logger);

            // Set up push logic for the destination.
            var nuGetPackagePusher = new NuGetPackagePusher(destinationUrlBuilder.GetNuGetPushUrl(), request.DestinationApiKey, destinationHttpSource, logger);
            var vsixPackagePusher = new MyGetVsixPackagePusher(destinationUrlBuilder.GetVsixPushUrl(), request.DestinationApiKey, destinationHttpSource, logger);

            // Set up the mirror logic.
            var nuGetPackageMirror = new NuGetPackageMirrorCommand(
                request.OverwriteExisting,
                request.IncludeNuGetSymbols,
                sourceDownloaderResource,
                sourceSymbolsPackageDownloader,
                destinationExistenceChecker,
                nuGetPackagePusher,
                settings,
                logger);

            var nuGetMirror = new NuGetMirrorCommand(
                request.MaxDegreeOfParallelism,
                nuGetPackageEnumerator,
                nuGetPackageMirror,
                logger);

            var vsixPackageMirror = new VsixPackageMirrorCommand(
                request.OverwriteExisting,
                sourceVsixPackageDownloader,
                destinationVsixPackageDownloader,
                vsixPackagePusher);

            var vsixMirror = new VsixMirrorCommand(
                request.MaxDegreeOfParallelism,
                vsixPackageEnumerator,
                vsixPackageMirror,
                logger);

            // Execute.
            if (request.IncludeNuGet)
            {
                await nuGetMirror.Execute(token);
            }

            if (request.IncludeVsix)
            {
                await vsixMirror.Execute(token);
            }
        }
    }
}
