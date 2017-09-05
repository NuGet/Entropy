using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SqlPerf
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }
        
        private const string BaseUrl = "https://localhost";
        private const int Iterations = 0;
        private static readonly List<SlowQuery> SlowQueries = new List<SlowQuery>
        {
            new SlowQuery(1, "52323791", "/api/v2/Packages/$count?$filter=IsAbsoluteLatestVersion"),
            new SlowQuery(2, "52323809", "/api/v2/Packages()?$filter=(startswith(tolower(Tags),'xrmtoolbox') and (tolower(Tags) ne 'xrmtoolbox')) and IsLatestVersion"),
            new SlowQuery(3, "52328671", "/api/v2/Packages()?$filter=((substringof('octopus.client', tolower(Id)) eq true or substringof('octopus.client', tolower(Title)) eq true or substringof('octopus.client', tolower(Description)) eq true or substringof('octopus.client', tolower(Summary)) eq true or substringof('octopus.client', tolower(Tags)) eq true) and (IsPrerelease eq true))"),
            new SlowQuery(4, "52354644", "/api/v2/Packages()?$orderby=Created%20desc&$top=10"),
            new SlowQuery(6, "53245266", "/api/v2/Packages()/$count?$filter=(LastUpdated gt datetime'2016-11-19T00:35:17.067') and (Published gt datetime'1900-01-01T00:00:00')&$orderby=LastUpdated"),
            new SlowQuery(7, "52328716", "/api/v2/Packages()?$filter=substringof('Newtonsoft.Json',Id)&$orderby=Created desc&$top=1"),
            new SlowQuery(9, "52360207", "/api/v2/Packages()?$filter=substringof('',Id)&$orderby=Created desc&$top=1"),
            new SlowQuery(10, "52327731", "/api/v2/Packages()?$filter=substringof('NETStandard.Library',Id)&$orderby=Created desc&$top=1"),
        };

        private static async Task MainAsync(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                await httpClient.GetStringAsync(BaseUrl);

                Console.WriteLine("Iteration,Order,Query ID,Total Seconds,Success");

                for (var i = 0; i <= Iterations; i++)
                {
                    for (var q = 0; q < SlowQueries.Count; q++)
                    {
                        var slowQuery = SlowQueries[q];
                        var odataUrl = BaseUrl + slowQuery.ODataUrl;
                        var sw = Stopwatch.StartNew();
                        TimeSpan elapsed;
                        try
                        {
                            await httpClient.GetStringAsync(odataUrl);
                        }
                        catch (Exception)
                        {
                            elapsed = sw.Elapsed;
                            q--;
                            Console.WriteLine($"{i},{slowQuery.Order},{slowQuery.QueryId},{elapsed.TotalSeconds},FALSE");
                            continue;
                        }

                        elapsed = sw.Elapsed;
                        Console.WriteLine($"{i},{slowQuery.Order},{slowQuery.QueryId},{elapsed.TotalSeconds},TRUE");
                    }
                }
            }                
        }
    }

    public class SlowQuery
    {
        public int Order { get; }
        public string QueryId { get; }
        public string ODataUrl { get; }

        public SlowQuery(int order, string queryId, string odataUrl)
        {
            Order = order;
            QueryId = queryId;
            ODataUrl = odataUrl;
        }
    }
}
