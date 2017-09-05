using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SplitLargeFiles
{
    public class Program
    {
        /// <summary>
        /// Size is in bytes.
        /// </summary>
        private const int DesiredFileSize = 20 * 1024 * 1024;
        private const string GzipExtension = ".gz";
        private const string FileIndexFormat = "_{0:D4}";

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var inputPath = Path.GetFullPath(@"C:\Users\jver\Downloads\test.txt");

            using (var inputStream = new FileStream(inputPath, FileMode.Open))
            {
                Console.WriteLine("Input parameters:");
                Console.WriteLine();
                Console.WriteLine($"  Desired outout file size (bytes): {DesiredFileSize}");
                Console.WriteLine($"  Input path: {inputPath}");
                Console.WriteLine();

                Console.Write("Gathering data about the input file...");
                var properties = await FileProperties.FromFileStreamAsync(inputStream);
                Console.WriteLine(" done.");

                // Generate the output path format.
                var outputPathFormat = GenerateOutputPathFormat(inputPath, properties.IsGzipped);

                // This is a rough estimate assuming that each line compresses to the same length.
                var fileCount = properties.Size / DesiredFileSize;
                if (fileCount == 0)
                {
                    fileCount = 1;
                }
                var linesPerFile = properties.LineCount / fileCount;

                Console.WriteLine();
                Console.WriteLine($"  Output path format: {outputPathFormat}");
                Console.WriteLine();
                Console.WriteLine($"  Input File encoding: {properties.Encoding}");
                Console.WriteLine($"  Is gzipped: {properties.IsGzipped}");
                Console.WriteLine($"  Input file size (bytes): {properties.Size}");
                Console.WriteLine($"  Input file line count: {properties.LineCount}");
                Console.WriteLine();
                Console.WriteLine($"  Output file count: {fileCount}");
                Console.WriteLine($"  Output file line count: {linesPerFile}");
                Console.WriteLine();

                Console.WriteLine("Splitting up files...");

                Stream readStream = inputStream;
                if (properties.IsGzipped)
                {
                    readStream = new GZipStream(inputStream, CompressionMode.Decompress);
                }

                using (var reader = new StreamReader(readStream, properties.Encoding))
                {
                    for (var fileIndex = 0; fileIndex < fileCount; fileIndex++)
                    {
                        var isLastFile = fileIndex >= fileCount - 1;
                        var outputPath = string.Format(outputPathFormat, fileIndex);

                        Console.Write($"[{fileIndex + 1}/{fileCount}] Writing {outputPath}...");
                        await WriteOutputFileAsync(properties, reader, linesPerFile, outputPath, isLastFile);
                        Console.WriteLine(" done.");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("The file has been split up.");
            }
        }

        private static string GenerateOutputPathFormat(string inputPath, bool isGzipped)
        {
            var directory = Path.GetDirectoryName(inputPath);
            var fileName = Path.GetFileName(inputPath);

            var gzipSuffix = string.Empty;
            if (isGzipped && inputPath.EndsWith(GzipExtension, StringComparison.OrdinalIgnoreCase))
            {
                gzipSuffix = Path.GetExtension(fileName);
                fileName = Path.GetFileNameWithoutExtension(fileName);
            }

            var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            return Path.Combine(directory, withoutExtension + FileIndexFormat + extension + gzipSuffix);
        }

        private static async Task WriteOutputFileAsync(FileProperties properties, StreamReader reader, long linesPerFile, string outputPath, bool isLastFile)
        {
            using (var outputStream = new FileStream(outputPath, FileMode.Create))
            {
                Stream writeStream = outputStream;
                if (properties.IsGzipped)
                {
                    writeStream = new GZipStream(writeStream, CompressionLevel.Optimal);
                }

                var lineIndex = 0;
                using (var writer = new StreamWriter(writeStream, properties.Encoding))
                {
                    string line;
                    while (((!isLastFile && lineIndex < linesPerFile) || isLastFile) &&
                           (line = await reader.ReadLineAsync()) != null)
                    {
                        lineIndex++;
                        await writer.WriteLineAsync(line);
                    }
                }
            }
        }
    }
}
