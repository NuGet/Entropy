using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyGetMirror
{
    public class VsixPackageMirrorCommand
    {
        private readonly IVsixPackageDownloader _destinationPackageDownloader;
        private readonly bool _overwriteExisting;
        private readonly IVsixPackagePusher _packagePusher;
        private readonly IVsixPackageDownloader _sourcePackageDownloader;

        public VsixPackageMirrorCommand(
            bool overwriteExisting,
            IVsixPackageDownloader sourcePackageDownloader,
            IVsixPackageDownloader destinationPackageDownloader,
            IVsixPackagePusher packagePusher)
        {
            _overwriteExisting = overwriteExisting;
            _sourcePackageDownloader = sourcePackageDownloader;
            _destinationPackageDownloader = destinationPackageDownloader;
            _packagePusher = packagePusher;
        }

        public async Task<bool> MirrorAsync(string id, string version, CancellationToken token)
        {
            var publish = true;

            if (!_overwriteExisting)
            {
                publish = !(await _destinationPackageDownloader.IsAvailableAsync(id, version, token));
            }

            if (publish)
            {
                publish = await _sourcePackageDownloader.ProcessAsync(
                    id,
                    version,
                    async streamResult =>
                    {
                        if (!streamResult.IsAvailable)
                        {
                            throw new InvalidOperationException($"The VSIX package '{id}' (version '{version}') is not available on the source.");
                        }

                        await _packagePusher.PushAsync(streamResult.Stream, token);

                        return true;
                    },
                    token);
            }

            return publish;
        }
    }
}
