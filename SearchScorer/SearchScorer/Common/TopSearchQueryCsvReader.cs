using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;

namespace SearchScorer.Common
{
    public static class TopSearchQueryCsvReader
    {
        /* This is the query that generates the data:

customMetrics
| where timestamp > ago(90d)
| where name == "BrowserSearchPage"
| where customDimensions.PageIndex == 0
| extend Query = trim("\\s", tostring(customDimensions.SearchTerm))
| distinct Query, session_Id
| summarize QueryCount = count() by Query
| order by QueryCount desc
| take 10000

            */

        public static IReadOnlyDictionary<string, int> Read(string path)
        {
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream))
            using (var csvReader = new CsvReader(streamReader))
            {
                return csvReader
                    .GetRecords<Record>()
                    .ToDictionary(x => x.Query, x => x.QueryCount);
            }
        }

        private class Record
        {
            public string Query { get; set; }
            public int QueryCount { get; set; }
        }
    }
}
