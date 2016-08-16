using NuGet.Protocol.Core.Types;

namespace MyGetMirror
{
    public class PackageEnumeratorContinuationToken
    {
        public string SearchTerm { get; set; }
        public SearchFilter SearchFilters { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}
