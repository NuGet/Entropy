using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CsvHelper;
using SearchScorer.Feedback;
using SearchScorer.IREvalutation;

namespace SearchScorer.Common
{
    public class GoogleAnalyticsSearchReferralsCsvReader
    {
        public static IReadOnlyDictionary<string, int> Read(string path)
        {
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream))
            using (var csvReader = new CsvReader(streamReader))
            {
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.IgnoreBlankLines = true;

                var output = new Dictionary<string, int>();

                csvReader.Read(); // comment
                csvReader.Read(); // comment 
                csvReader.Read(); // comment
                csvReader.Read(); // comment
                csvReader.Read(); // comment
                csvReader.Read(); // empty line
                csvReader.ReadHeader();

                while (csvReader.Read())
                {
                    var landingPage = csvReader.GetField<string>("Landing Page");
                    var landingUri = new Uri("http://example" + landingPage);
                    var queryString = HttpUtility.ParseQueryString(landingUri.Query);

                    // Skip queries where we are not hitting the first page.
                    if (int.TryParse(queryString["page"], out var page) && page != 1)
                    {
                        continue;
                    }

                    var searchTerm = csvReader.GetField<string>("Search Term");
                    var sessions = int.Parse(csvReader.GetField<string>("Sessions").Replace(",", string.Empty));

                    if (output.TryGetValue(searchTerm, out var existingSessions))
                    {
                        output[searchTerm] += sessions;
                    }
                    else
                    {
                        output.Add(searchTerm, sessions);
                    }
                }

                return output;
            }
        }

        private class Record
        {
            public string LandingPage { get; set; }
            public string SearcTerm { get; set; }
            public int Sessions { get; set; }
        }
    }

    public static class CuratedSearchQueriesCsvReader
    {
        public static IReadOnlyList<CuratedSearchQuery> Read(string path)
        {
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream))
            using (var csvReader = new CsvReader(streamReader))
            {
                var existingScores = new Dictionary<string, Dictionary<string, int>>();

                var output = new List<CuratedSearchQuery>();
                int lineNumber = 1; // The header is read automatically
                foreach (var record in csvReader.GetRecords<Record>())
                {
                    lineNumber++;

                    var searchQuery = record.SearchQuery.Trim();
                    if (existingScores.ContainsKey(searchQuery))
                    {
                        throw new InvalidOperationException($"The search query '{searchQuery}' is a duplicate in file, line {lineNumber}: {path}");
                    }

                    var pairs = new[]
                    {
                        new { PackageId = record.ID0, Score = record.S0 },
                        new { PackageId = record.ID1, Score = record.S1 },
                        new { PackageId = record.ID2, Score = record.S2 },
                        new { PackageId = record.ID3, Score = record.S3 },
                        new { PackageId = record.ID4, Score = record.S4 },
                    };

                    var packageIdToScore = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in pairs)
                    {
                        var packageId = pair.PackageId?.Trim();
                        if (!string.IsNullOrWhiteSpace(packageId))
                        {
                            if (packageIdToScore.ContainsKey(packageId))
                            {
                                throw new InvalidOperationException($"The package ID '{packageId}' is duplicate for search query '{searchQuery}' in file, line {lineNumber}: {path}");
                            }

                            if (string.IsNullOrWhiteSpace(pair.Score))
                            {
                                throw new InvalidOperationException($"The package ID '{packageId}' has a missing score for search query '{searchQuery}' in file, line {lineNumber}: {path}");
                            }

                            if (!int.TryParse(pair.Score.Trim(), out var score))
                            {
                                throw new InvalidOperationException($"The package ID '{packageId}' has an invalid score for search query '{searchQuery}' in file, line {lineNumber}: {path}");
                            }

                            if (score < 1 || score > RelevancyScoreBuilder.MaximumRelevancyScore)
                            {
                                throw new InvalidOperationException(
                                    $"The package ID '{packageId}' has a score out of range [1, " +
                                    $"{RelevancyScoreBuilder.MaximumRelevancyScore}] for search query " +
                                    $"'{searchQuery}' in file, line {lineNumber}: {path}");
                            }

                            packageIdToScore.Add(packageId, score);
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(pair.Score))
                            {
                                throw new InvalidOperationException($"There is a score without a package ID for search query '{searchQuery}' in file, line {lineNumber}: {path}");
                            }
                        }
                    }

                    if (packageIdToScore.Any())
                    {
                        existingScores.Add(searchQuery, packageIdToScore);

                        output.Add(new CuratedSearchQuery(
                            searchQuery,
                            packageIdToScore));
                    }
                    else
                    {
                        Console.WriteLine($"[ WARN ] Skipping at search query '{searchQuery}' since it has no scores.");
                    }
                }

                return output;
            }
        }

        private class Record
        {
            public string SearchQuery { get; set; }
            public string ID0 { get; set; }
            public string S0 { get; set; }
            public string ID1 { get; set; }
            public string S1 { get; set; }
            public string ID2 { get; set; }
            public string S2 { get; set; }
            public string ID3 { get; set; }
            public string S3 { get; set; }
            public string ID4 { get; set; }
            public string S4 { get; set; }
        }
    }
}
