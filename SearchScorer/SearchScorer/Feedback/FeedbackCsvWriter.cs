using System;
using System.IO;
using CsvHelper;

namespace SearchScorer.Feedback
{
    public class FeedbackCsvWriter : IDisposable
    {
        private bool _opened;
        private int _number;
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        private CsvWriter _csvWriter;

        public FeedbackCsvWriter()
        {
            _opened = false;
            _number = 0;
        }

        public void WriteResult(FeedbackResult result)
        {
            if (!_opened)
            {
                _fileStream = new FileStream("FeedbackItems.csv", FileMode.Create);
                _streamWriter = new StreamWriter(_fileStream);
                _csvWriter = new CsvWriter(_streamWriter);

                _csvWriter.WriteField("Feedback Item Number");
                _csvWriter.WriteField("Source");
                _csvWriter.WriteField("Disposition");
                _csvWriter.WriteField("Buckets");
                _csvWriter.WriteField("Search Query");
                _csvWriter.WriteField("Most Relevant Package IDs");
                _csvWriter.WriteField("Result");
                _csvWriter.WriteField("Control Result Bucket");
                _csvWriter.WriteField("Treatment Result Bucket");
                _csvWriter.WriteField("Control Result Index");
                _csvWriter.WriteField("Treatment Result Index");
                _csvWriter.WriteField("Result Index Delta");
                _csvWriter.NextRecord();

                _opened = true;
            }

            _number++;

            _csvWriter.WriteField(_number);
            _csvWriter.WriteField(result.FeedbackItem.Source);
            _csvWriter.WriteField(result.FeedbackItem.Disposition);
            _csvWriter.WriteField(string.Join(" | ", result.FeedbackItem.Buckets));
            _csvWriter.WriteField(result.FeedbackItem.Query);
            _csvWriter.WriteField(string.Join(" | ", result.FeedbackItem.MostRelevantPackageIds));
            _csvWriter.WriteField(result.Type);
            _csvWriter.WriteField(result.ControlResult.ResultIndexBucket);
            _csvWriter.WriteField(result.TreatmentResult.ResultIndexBucket);
            _csvWriter.WriteField(result.ControlResult.ResultIndex);
            _csvWriter.WriteField(result.TreatmentResult.ResultIndex);

            if (result.ControlResult.ResultIndex.HasValue && result.TreatmentResult.ResultIndex.HasValue)
            {
                _csvWriter.WriteField(result.TreatmentResult.ResultIndex.Value - result.ControlResult.ResultIndex.Value);
            }
            else
            {
                _csvWriter.WriteField(string.Empty);
            }

            _csvWriter.NextRecord();
        }

        public void Dispose()
        {
            _csvWriter?.Dispose();
            _streamWriter?.Dispose();
            _fileStream?.Dispose();
        }
    }
}
