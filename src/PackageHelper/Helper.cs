using CsvHelper;
using CsvHelper.Configuration;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PackageHelper
{
    static class Helper
    {
        private const string RootMarker = "discover-packages.ps1";

        public static void AppendCsv<T>(string resultsPath, T record)
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

        public static string GetLogTimestamp()
        {
            return DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssffff");
        }

        public static string GetExcelTimestamp(DateTimeOffset input)
        {
            return input.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static SourceCacheContext GetCacheContext()
        {
            return new SourceCacheContext
            {
                DirectDownload = true,
                NoCache = true,
            };
        }

        public static bool TryFindRoot(out string dir)
        {
            dir = Directory.GetCurrentDirectory();
            var tried = new List<string>();
            bool foundRoot;
            do
            {
                tried.Add(dir);
                foundRoot = Directory
                    .EnumerateFiles(dir)
                    .Select(filePath => Path.GetFileName(filePath))
                    .Contains(RootMarker);

                if (!foundRoot)
                {
                    dir = Path.GetDirectoryName(dir);
                }
            }
            while (dir != null && !foundRoot);

            if (!foundRoot)
            {
                Console.WriteLine($"Could not find {RootMarker} in the current or parent directories.");
                Console.WriteLine("Tried:");
                foreach (var path in tried)
                {
                    Console.WriteLine($"  {path}");
                }

                dir = null;
                return false;
            }

            return true;
        }
    }
}
