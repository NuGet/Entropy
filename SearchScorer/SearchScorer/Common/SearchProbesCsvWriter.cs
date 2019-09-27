using System.Collections.Generic;
using System.IO;
using CsvHelper;

namespace SearchScorer.Common
{
    public static class SearchProbesCsvWriter
    {
        public static void Write(string path, IEnumerable<SearchProbesRecord> scores)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream))
            using (var csvWriter = new CsvWriter(streamWriter))
            {
                csvWriter.WriteHeader<SearchProbesRecord>();
                csvWriter.NextRecord();
                csvWriter.WriteRecords(scores);
            }
        }
    }

    public class SearchProbeTest
    {
        public double PackageIdWeight { get; set; }
        public double TokenizedPackageIdWeight { get; set; }
        public double TagsWeight { get; set; }
        public double DownloadScoreBoost { get; set; }
    }

    public class SearchProbesRecord : SearchProbeTest
    {
        public double CuratedSearchScore { get; set; }
        public double FeedbackScore { get; set; }
    }
}
