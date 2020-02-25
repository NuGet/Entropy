using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json;

namespace ExportApplicationInsights
{
    class Program
    {
        private const string TimestampFormat = "yyyy.MM.dd.HH.mm.ss.FFFFFFF";

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var applicationId = "";
            var apiKey = "";

            var eventName = "BrowserSearchPage";
            var minTimestamp = DateTimeOffset.Parse("2019-07-02T18:57:00Z");
            var maxTimestamp = DateTimeOffset.Parse("2019-07-29T20:00:00Z");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                int resultCount;
                var currentMinTimestamp = GetCurrentMaxTimestamp(eventName) ?? minTimestamp;

                do
                {
                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri($"https://api.applicationinsights.io/v1/apps/{applicationId}/query");

                        var query = $@"
let minTimestamp = todatetime('{minTimestamp:O}');
let maxTimestamp = todatetime('{maxTimestamp:O}');
customMetrics
| where timestamp > minTimestamp
| where timestamp < maxTimestamp
| where name == '{eventName}'
| where timestamp > todatetime('{currentMinTimestamp:O}')
| order by timestamp asc
";
                        var queryObj = new { query };
                        var queryJson = JsonConvert.SerializeObject(queryObj);

                        request.Content = new StringContent(queryJson, Encoding.UTF8, "application/json");

                        Console.WriteLine($"Requesting '{eventName}' ({currentMinTimestamp:O}, {maxTimestamp:O})...");
                        using (var response = await httpClient.SendAsync(request))
                        {
                            response.EnsureSuccessStatusCode();
                            var responseJson = await response.Content.ReadAsStringAsync();
                            var queryResponse = JsonConvert.DeserializeObject<QueryResponse>(responseJson, new JsonSerializerSettings
                            {
                                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                                DateParseHandling = DateParseHandling.DateTimeOffset,
                                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                            });

                            resultCount = queryResponse.Tables.Single().Rows.Count;

                            Console.WriteLine($"Got {resultCount} records.");

                            if (resultCount > 0)
                            {
                                currentMinTimestamp = queryResponse
                                    .Tables
                                    .Single()
                                    .Rows
                                    .Select(x => x[0])
                                    .Cast<DateTimeOffset>()
                                    .Max();

                                WriteDataFile(eventName, currentMinTimestamp, queryResponse);
                            }
                        }
                    }
                }
                while (resultCount > 0);
            }
        }

        static DateTimeOffset? GetCurrentMaxTimestamp(string eventName)
        {
            DateTimeOffset? maxTimestamp = null;
            foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.csv"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (!fileName.StartsWith(eventName + "."))
                {
                    continue;
                }

                var unparsed = fileName.Substring(eventName.Length + 1);
                if (DateTimeOffset.TryParseExact(unparsed, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    if (maxTimestamp == null || parsed > maxTimestamp.Value)
                    {
                        maxTimestamp = parsed;
                    }
                }
                else
                {
                    Console.WriteLine($"Could not parse file name '{fileName}'. Ignoring.");
                }
            }

            return maxTimestamp;
        }

        static void WriteDataFile(string eventName, DateTimeOffset maxTimestamp, QueryResponse response)
        {
            var table = response.Tables.Single();

            var fileName = $"{eventName}.{maxTimestamp.ToString(TimestampFormat)}.csv";

            Console.WriteLine($"Writing data file '{fileName}'...");

            using (var fileStream = new FileStream(fileName, FileMode.CreateNew))
            using (var streamWriter = new StreamWriter(fileStream))
            using (var csvWriter = new CsvWriter(streamWriter))
            {
                foreach (var column in table.Columns)
                {
                    csvWriter.WriteField(column.Name);
                }

                csvWriter.NextRecord();

                foreach (var row in table.Rows)
                {
                    foreach (var value in row)
                    {
                        if (value is DateTimeOffset dateTimeOffset)
                        {
                            csvWriter.WriteField(dateTimeOffset.ToString("O"));
                        }
                        else
                        {
                            csvWriter.WriteField(value);
                        }
                    }

                    csvWriter.NextRecord();
                }
            }
        }
    }

    public class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public List<object[]> Rows { get; set; }
    }

    public class Column
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class QueryResponse
    {
        public List<Table> Tables { get; set; }
    }
}
