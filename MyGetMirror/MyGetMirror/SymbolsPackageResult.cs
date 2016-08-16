using System.IO;

namespace MyGetMirror
{
    public class StreamResult
    {
        public bool IsAvailable { get; set; }
        public Stream Stream { get; set; }
    }
}
