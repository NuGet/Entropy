using System;
using System.IO;
using System.IO.Compression;

namespace NuGetValidator.Utility
{
    public class VsixUtility
    {
        public static void ExtractVsix(string vsixPath, string extractedVsixPath)
        {
            CleanExtractedFiles(extractedVsixPath);

            Console.WriteLine($"Extracting {vsixPath} to {extractedVsixPath}");

            ZipFile.ExtractToDirectory(vsixPath, extractedVsixPath);

            Console.WriteLine($"Done Extracting...");
        }

        public static void CleanExtractedFiles(string path)
        {
            Console.WriteLine("Cleaning up the extracted files");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
