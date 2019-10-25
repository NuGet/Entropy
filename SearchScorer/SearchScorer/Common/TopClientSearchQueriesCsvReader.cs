using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;

namespace SearchScorer.Common
{
    public static class TopClientSearchQueriesCsvReader
    {
        /* This is the query that generates the data:

let lookup = materialize (cluster("CLUSTER").database("DATABASE").TABLE);
RawEventsNuGet
| where AdvancedServerTimestampUtc > ago(60d)
| where EventName == "vs/nuget/search"
| extend HashedQuery = tostring(Properties['vs.nuget.query'])
| join kind=leftouter (lookup) on HashedQuery
| extend Query = iff(isnull(Count), "<UNKNOWN QUERY>", Query)
| extend Query = trim("[\\s\\.\\-_]+", Query)
| summarize QueryCount = count() by Query
| order by QueryCount desc
| take 2000

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
