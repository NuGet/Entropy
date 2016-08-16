using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;

namespace MyGetMirror
{
    public interface INuGetSymbolsPackageDownloader
    {
        Task<bool> IsAvailableAsync(PackageIdentity identity, CancellationToken token);
        Task<T> ProcessAsync<T>(PackageIdentity identity, Func<SymbolsPackageResult, Task<T>> processAsync, CancellationToken token);
    }
}
