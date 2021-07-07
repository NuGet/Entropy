using System;

namespace NuGetPackageAnalyzerFunction
{
    /// <summary>
    /// The formatted queue message expected to be received from the queue.
    /// Add JSON Serialization properties if required.
    /// </summary>
    public class CatalogLeafMessage
    {
        public string Id;

        public string Version;

        public DateTime Created;
    }
}
