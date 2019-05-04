using NuGet.Versioning;

namespace TestIngestionPerf
{
    public class TestPackage
    {
        public TestPackage(string id, NuGetVersion version, byte[] bytes)
        {
            Id = id;
            Version = version;
            Bytes = bytes;
        }

        public string Id { get; }
        public NuGetVersion Version { get; }
        public byte[] Bytes { get; }
    }
}
