using System;
using System.IO;

namespace NuGetValidator.Utility
{
    public class TemporaryDirectory : IDisposable
    {
        public string Path { get; }
        private DirectoryInfo DirectoryInfo { get; }

        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            DirectoryInfo =  Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }

        public override string ToString()
        {
            return Path;
        }

        public static implicit operator string(TemporaryDirectory directory)
        {
            return directory.Path;
        }
    }
}
