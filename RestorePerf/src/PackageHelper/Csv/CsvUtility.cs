using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using PackageHelper.Commands;

namespace PackageHelper.Csv
{
    public static class CsvUtility
    {
        public static void Append<T>(string resultsPath, T record)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = !File.Exists(resultsPath),
            };

            using (var fileStream = new FileStream(resultsPath, FileMode.Append))
            using (var writer = new StreamWriter(fileStream))
            using (var csv = new CsvWriter(writer, csvConfig))
            {
                csv.WriteRecords(new[] { record });
            }
        }

        public static IEnumerable<RestoreResultRecord> EnumerateRestoreResults(string dir)
        {
            Console.WriteLine("Parsing restore result files...");

            var fileCount = 0;
            foreach (var resultPath in Directory.EnumerateFiles(dir, "results-*.csv"))
            {
                fileCount++;
                using (var streamReader = new StreamReader(resultPath))
                using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                {
                    foreach (var record in csvReader.GetRecords<RestoreResultRecord>())
                    {
                        yield return record;
                    }
                }
            }

            Console.WriteLine($"{fileCount} restore result files were parsed.");
        }

        public static IEnumerable<ReplayResultRecord> EnumerateReplayResults(string dir)
        {
            Console.WriteLine("Parsing the replay result files...");

            var fileCount = 0;
            var replayResultsPath = Path.Combine(dir, ReplayRequestGraph.ResultFileName);
            if (File.Exists(replayResultsPath))
            {
                fileCount++;
                using (var reader = new StreamReader(replayResultsPath))
                using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    foreach (var record in csvReader.GetRecords<ReplayResultRecord>())
                    {
                        yield return record;
                    }
                }
            }

            Console.WriteLine($"{fileCount} replay result files were parsed.");
        }
    }
}
