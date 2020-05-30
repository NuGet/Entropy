using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

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
    }
}
