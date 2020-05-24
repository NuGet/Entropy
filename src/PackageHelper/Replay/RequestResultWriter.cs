using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using CsvHelper;

namespace PackageHelper.Replay
{

    class RequestResultWriter : IRequestResultWriter, IDisposable
    {
        private readonly BlockingCollection<CsvRecord> _records = new BlockingCollection<CsvRecord>();
        private readonly FileStream _stream;
        private readonly StreamWriter _streamWriter;
        private readonly CsvWriter _csvWriter;
        private readonly Thread _thread;

        public RequestResultWriter(string path)
        {
            _stream = new FileStream(path, FileMode.Create);
            _streamWriter = new StreamWriter(_stream);
            _csvWriter = new CsvWriter(_streamWriter, CultureInfo.InvariantCulture);
            _thread = new Thread(Consume) { IsBackground = true };
            _thread.Start();
        }

        public void OnResponse(RequestNode node, HttpStatusCode statusCode, TimeSpan headerDuration, TimeSpan bodyDuration)
        {
            _records.Add(new CsvRecord
            {
                Url = node.StartRequest.Url,
                StatusCode = (int)statusCode,
                HeaderDurationMs = headerDuration.TotalMilliseconds,
                BodyDurationMs = bodyDuration.TotalMilliseconds,
            });
        }

        private void Consume()
        {
            while (!_records.IsCompleted)
            {
                CsvRecord record = null;
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
            _thread.Join();
            _csvWriter.Dispose();
            _streamWriter.Dispose();
            _stream.Dispose();
        }

        private class CsvRecord
        {
            public string Url { get; set; }
            public int StatusCode { get; set; }
            public double HeaderDurationMs { get; set; }
            public double BodyDurationMs { get; set; }
        }
    }
}
