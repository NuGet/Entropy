using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading;
using CsvHelper;

namespace PackageHelper.Csv
{
    class BackgroundCsvWriter<T> : IDisposable where T : class
    {
        private readonly BlockingCollection<T> _records = new BlockingCollection<T>();
        private readonly FileStream _stream;
        private readonly StreamWriter _streamWriter;
        private readonly CsvWriter _csvWriter;
        private readonly Thread _thread;
        private readonly GZipStream _gzipStream;

        public BackgroundCsvWriter(string path, bool gzip)
        {
            try
            {
                _stream = new FileStream(path, FileMode.Create);
                if (gzip)
                {
                    _gzipStream = new GZipStream(_stream, CompressionLevel.Optimal);
                    _streamWriter = new StreamWriter(_gzipStream);
                }
                else
                {
                    _streamWriter = new StreamWriter(_stream);
                }
                _csvWriter = new CsvWriter(_streamWriter, CultureInfo.InvariantCulture);
                _thread = new Thread(Consume) { IsBackground = true };
                _thread.Start();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Add(T record)
        {
            _records.Add(record);
        }

        private void Consume()
        {
            while (!_records.IsCompleted)
            {
                T record = null;
                try
                {
                    record = _records.Take();
                }
                catch (InvalidOperationException)
                {
                }

                if (record != null)
                {
                    _csvWriter.WriteRecords(new[] { record });
                }
            }
        }

        public void Dispose()
        {
            _records.CompleteAdding();
            if (_thread.IsAlive)
            {
                _thread.Join();
            }
            _csvWriter.Dispose();
            _streamWriter.Dispose();
            _gzipStream?.Dispose();
            _stream.Dispose();
        }
    }
}
