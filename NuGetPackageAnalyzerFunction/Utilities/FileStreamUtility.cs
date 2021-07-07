using System.IO;

namespace NuGetPackageAnalyzerFunction.Utilities
{
    public static class FileStreamUtility
    {
        public const int BufferSize = 8192;

        public static FileStream GetTemporaryFile()
        {
            return new FileStream(
                Path.GetTempFileName(),
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                BufferSize,
                FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        }

        public static FileStream OpenTemporaryFile(string fileName)
            => new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                BufferSize,
                FileOptions.DeleteOnClose | FileOptions.Asynchronous);
    }
}
