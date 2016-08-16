using System.Collections.Generic;
using NuGet.Packaging.Core;

namespace MyGetMirror
{
    public class PackageEnumeratorResult
    {
        public bool HasMoreResults { get; set; }
        public PackageEnumeratorContinuationToken ContinuationToken { get; set; }
        public IReadOnlyList<PackageIdentity> PackageIdentities { get; set; }
    }
}
