using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace SplitLargeFiles
{
    public class FileProperties
    {
        private const int BufferSize = 16 * 1024;

        public FileProperties(string path, long size, bool isGzipped, Encoding encoding, int lineCount)
        {
            Path = path;
            Size = size;
            IsGzipped = isGzipped;
            Encoding = encoding;
            LineCount = lineCount;
        }

        public string Path { get; }
        public long Size { get; }
        public bool IsGzipped { get; }
        public Encoding Encoding { get; }
        public int LineCount { get; }

        public static async Task<FileProperties> FromFileStreamAsync(FileStream stream)
        {
            var path = stream.Name;
            var size = stream.Length;
            var isGzipped = DetectGzip(stream);
            var encoding = await DetectEncodingAsync(isGzipped, stream);
            var lineCount = await CountLinesAsync(isGzipped, encoding, stream);

            return new FileProperties(
                path,
                size,
                isGzipped,
                encoding,
                lineCount);
        }

        private static byte[] GetLeadingBytes(Stream stream, int count)
        {
            var initialPosition = stream.Position;
            stream.Position = 0;
            var buffer = new byte[count];
            var read = stream.Read(buffer, 0, buffer.Length);
            var bytes = new byte[read];
            Buffer.BlockCopy(buffer, 0, bytes, 0, read);
            stream.Position = initialPosition;
            return bytes;
        }

        private static bool DetectGzip(Stream stream)
        {
            var header = GetLeadingBytes(stream, 2);
            if (header.Length < 2 || header[0] != 0x1f || header[1] != 0x8b)
            {
                return false;
            }

            var initialPosition = stream.Position;
            stream.Position = 0;
            try
            {
                using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
                {
                    var buffer = new byte[128];
                    gzipStream.Read(buffer, 0, buffer.Length);
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
            stream.Position = initialPosition;

            return true;
        }

        private static async Task<Encoding> DetectEncodingAsync(bool isGzipped, Stream stream)
        {
            return await ProcesStreamAsync(
                isGzipped,
                stream,
                readStream =>
                {
                    using (var reader = new StreamReader(
                        readStream,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: true,
                        bufferSize: BufferSize,
                        leaveOpen: true))
                    {
                        reader.Peek();
                        return Task.FromResult(reader.CurrentEncoding);
                    }
                });
        }

        private static async Task<int> CountLinesAsync(bool isGzipped, Encoding encoding, Stream stream)
        {
            return await ProcesStreamAsync(
                isGzipped,
                stream,
                async readStream =>
                {
                    var lineCount = 0;

                    using (var reader = new StreamReader(
                        readStream,
                        encoding,
                        detectEncodingFromByteOrderMarks: false,
                        bufferSize: BufferSize,
                        leaveOpen: true))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            lineCount++;
                        }
                    }

                    return lineCount;
                });
        }

        private static async Task<T> ProcesStreamAsync<T>(bool isGzipped, Stream stream, Func<Stream, Task<T>> processAsync)
        {
            var initialPosition = stream.Position;
            stream.Position = 0;

            var readStream = stream;
            if (isGzipped)
            {
                readStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
            }

            var output = default(T);
            try
            {
                output = await processAsync(readStream);
            }
            finally
            {
                if (isGzipped)
                {
                    readStream.Dispose();
                }
            }

            stream.Position = initialPosition;
            return output;
        }
    }
}
