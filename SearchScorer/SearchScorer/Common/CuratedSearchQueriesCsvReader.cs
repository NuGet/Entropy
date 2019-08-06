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
                // Using case sensitive comparison, a search query should only appear once.
                var caseSensitive = new HashSet<string>();

                // Using case insensitive comparison, PackageIdX and ScoreX should only be set on the first search
                // query in the file. It's a reasonable user expectation for search to be case insensitive. It is not
                // in reality (we have camel case splitting) but the expected results for scoring purposes should be
                // the same for all casings and the expected package IDs score only be defined once.
                var existingScores = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

                var output = new List<CuratedSearchQuery>();
                int lineNumber = 1; // The header is read automatically
                foreach (var record in csvReader.GetRecords<Record>())
                {
                    lineNumber++;

                    var searchQuery = record.SearchQuery.Trim();
                    if (!caseSensitive.Add(searchQuery))
                    {
                        throw new InvalidOperationException($"The search query '{searchQuery}' is a duplicate in file, line {lineNumber}: {path}");
                    }

                    var pairs = new[]
                    {
                        new { PackageId = record.PackageId0, Score = record.Score0 },
                        new { PackageId = record.PackageId1, Score = record.Score1 },
                        new { PackageId = record.PackageId2, Score = record.Score2 },
                        new { PackageId = record.PackageId3, Score = record.Score3 },
                        new { PackageId = record.PackageId4, Score = record.Score4 },
                        new { PackageId = record.PackageId5, Score = record.Score5 },
                        new { PackageId = record.PackageId6, Score = record.Score6 },
                        new { PackageId = record.PackageId7, Score = record.Score7 },
                        new { PackageId = record.PackageId8, Score = record.Score8 },
                        new { PackageId = record.PackageId9, Score = record.Score9 },
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

                    if (existingScores.TryGetValue(searchQuery, out var existingPackageIdToScore))
                    {
                        if (packageIdToScore.Any())
                        {
                            throw new InvalidOperationException($"There scores for case insensitive search query '{searchQuery}' are defined multiple time in file, line {lineNumber}: {path}");
                        }

                        output.Add(new CuratedSearchQuery(
                            record.Source,
                            searchQuery,
                            existingPackageIdToScore));
                    }
                    else
                    {
                        if (packageIdToScore.Any())
                        {
                            existingScores.Add(searchQuery, packageIdToScore);

                            output.Add(new CuratedSearchQuery(
                                record.Source,
                                searchQuery,
                                packageIdToScore));
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: Skipping search query '{searchQuery}' since it has no scores.");
                        }
                    }
                }

                return output;
            }
        }

        private class Record
        {
            public SearchQuerySource Source { get; set; }
            public string SearchQuery { get; set; }
            public string PackageId0 { get; set; }
            public string Score0 { get; set; }
            public string PackageId1 { get; set; }
            public string Score1 { get; set; }
            public string PackageId2 { get; set; }
            public string Score2 { get; set; }
            public string PackageId3 { get; set; }
            public string Score3 { get; set; }
            public string PackageId4 { get; set; }
            public string Score4 { get; set; }
            public string PackageId5 { get; set; }
            public string Score5 { get; set; }
            public string PackageId6 { get; set; }
            public string Score6 { get; set; }
            public string PackageId7 { get; set; }
            public string Score7 { get; set; }
            public string PackageId8 { get; set; }
            public string Score8 { get; set; }
            public string PackageId9 { get; set; }
            public string Score9 { get; set; }
        }
    }
}
