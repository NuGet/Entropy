namespace MyGetMirror
{
    public class FeedMirrorRequest
    {
        public string PackagesDirectory { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string DestinationApiKey { get; set; }
        public bool IncludeNuGet { get; set; }
        public bool IncludeVsix { get; set; }
        public bool IncludeNuGetSymbols { get; set; }
        public bool OverwriteExisting { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
    }
}
