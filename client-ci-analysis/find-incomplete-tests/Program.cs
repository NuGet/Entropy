using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using find_incomplete_tests.Model;
using NetworkManager;

namespace find_incomplete_tests
{
    class Program
    {
        private static Regex regex = new Regex(@"^.*\[xUnit.net [0-9\:\.]*\](?<test>.*)\[(?<status>STARTING|FINISHED)\]");


        static async Task Main()
        {
            var resultFileNames = new[] { "official_builds.json", "private_builds.json" };

            var results = new Dictionary<string, List<BuildInfo>>();

            foreach (var resultFileName in resultFileNames)
            {
                var fileContents = File.ReadAllText(resultFileName);
                var builds = JsonSerializer.Deserialize<List<BuildInfo>>(fileContents);
                results.Add(resultFileName, builds);
            }

            var userAgent = new ProductInfoHeaderValue("zivkan", "0.1");
            var cacheDirectory = Path.Combine(Path.GetTempPath(), "nuget-client-ci-analysis");

            var qwerty = new Dictionary<string, List<(Build Build, RecordTracker TimeLine)>>();

            var httpManagerOptions = new HttpManagerOptions(userAgent, cacheDirectory);
            using (var httpManager = await HttpManager.CreateAsync(httpManagerOptions))
            {
                var accountName = Environment.GetEnvironmentVariable("AzDO_ACCOUNT");
                var personalAccessToken = Environment.GetEnvironmentVariable("AzDO_PAT");
                var cred = new AuthenticationHeaderValue("BASIC", Convert.ToBase64String(Encoding.ASCII.GetBytes(accountName + ":" + personalAccessToken)));
                httpManager.AddCredential("https://dev.azure.com/DevDiv/", cred);

                foreach (var (ciBuild, builds) in results)
                {
                    var buildList = new List<(Build Build, RecordTracker? TimeLine)>();

                    foreach (var build in builds)
                    {
                        var url = "https://dev.azure.com/DevDiv/DevDiv/_apis/build/builds/" + build.buildId + "?api-version=6.0";

                        Build buildData = await GetBuildData(url, httpManager);
                        RecordTracker? timeline = await GetTimeLine(buildData, httpManager);
                        buildList.Add((buildData, timeline));

                        Console.WriteLine($"{buildList.Count}/{builds.Count}");
                    }

                    qwerty.Add(ciBuild, buildList);
                }

                var linuxTestTimeouts = qwerty.SelectMany(q => q.Value)
                    .Select(q => q.TimeLine)
                    .Select(GetLinuxTestTask)
                    .Where(r => r != null && r.Record.result == "canceled")
                    .ToList();
                int tasksTimedOut = 0;
                var incompleteTestCounts = new Dictionary<string, uint>();
                foreach (var build in linuxTestTimeouts)
                {
                    tasksTimedOut++;
                    Console.WriteLine($"{tasksTimedOut}/{linuxTestTimeouts.Count}");
                    using (var logStream = await httpManager.GetAsync(build.Record.log.url))
                    using (var streamReader = new StreamReader(logStream))
                    {
                        var incompleteTests = await FindIncompleteTests(streamReader);

                        foreach (var test in incompleteTests)
                        {
                            if (!incompleteTestCounts.TryGetValue(test, out uint count))
                            {
                                count = 0;
                            }
                            count++;
                            incompleteTestCounts[test] = count;
                        }

                    }
                }

                Console.WriteLine($"{tasksTimedOut} timed out linux tests");
                foreach (var kvp in incompleteTestCounts.OrderByDescending(k => k.Value))
                {
                    Console.WriteLine($"{kvp.Value} {kvp.Key}");
                }
            }

            //if (args.Length == 0)
            //{
            //    Console.WriteLine("Pass one or more filenames", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            //    return;
            //}

            //foreach (var file in args)
            //{
            //    bool error = false;
            //    if (!File.Exists(file))
            //    {
            //        Console.WriteLine("File does not exist: " + file);
            //        error = true;
            //    }
            //    if (error)
            //    {
            //        return;
            //    }
            //}

            //var tasks = new List<Task<HashSet<string>>>(args.Length);
            //foreach (var file in args)
            //{
            //    tasks.Add(FindIncompleteTests(file));
            //}

            //await Task.WhenAll(tasks);

            //var counts = tasks.SelectMany(t => t.Result)
            //    .GroupBy(h => h)
            //    .Select(g => new { Test = g.Key, Count = g.Count() })
            //    .OrderByDescending(g => g.Count);

            //foreach (var group in counts)
            //{
            //    Console.WriteLine("{0}: {1}", group.Count, group.Test);
            //}
        }

        private static RecordTracker? GetLinuxTestTask(RecordTracker arg)
        {
            if (arg == null)
            {
                return null;
            }

            var phase = arg.Children.Values.SingleOrDefault(r => r.Record.name == "Tests_On_Linux");
            if (phase == null)
            {
                return null;
            }

            var job = phase.Children.Values.SingleOrDefault(r => r.Record.name == "Tests_On_Linux");
            if (job == null)
            {
                return null;
            }

            var task = job.Children.Values.SingleOrDefault(r => r.Record.name == "Run Tests");
            if (task == null)
            {
                return null;
            }

            return task;
        }

        private static async Task<RecordTracker?> GetTimeLine(Build buildData, HttpManager httpManager)
        {
            if (buildData.validationResults.Any(v=>v.result == "error"))
            {
                // yaml had syntax error, pipeline wasn't run (no timeline)
                return null;
            }

            if (!buildData.links.TryGetValue("timeline", out Build.Link timeLineLink))
            {
                throw new Exception();
            }

            using (var response = await httpManager.GetAsync(timeLineLink.href))
            {
                string json;
                using (var streamReader = new StreamReader(response))
                {
                    json = streamReader.ReadToEnd();
                }
                Timeline timeline;
                try
                {
                    timeline = JsonSerializer.Deserialize<Timeline>(json);
                }
                catch (Exception e)
                {
                    throw;
                }

                var deferred = new List<Timeline.Record>();
                var records = new Dictionary<string, RecordTracker>();

                // pass 1
                foreach (var record in timeline.records)
                {
                    if (string.IsNullOrEmpty(record.parentId))
                    {
                        var track = new RecordTracker()
                        {
                            Record = record,
                            Parent = null
                        };
                        records.Add(record.id, track);
                    }
                    else if (records.TryGetValue(record.parentId, out var parent))
                    {
                        var track = new RecordTracker()
                        {
                            Record = record,
                            Parent = parent
                        };
                        records.Add(record.id, track);
                        parent.Children.Add(record.order, track);
                    }
                    else
                    {
                        deferred.Add(record);
                    }
                }

                // pass 2
                var processed = new List<Timeline.Record>();
                while (deferred.Count > 0)
                {
                    processed.Clear();

                    foreach (var record in deferred)
                    {
                        if (records.TryGetValue(record.parentId, out var parent))
                        {
                            var track = new RecordTracker()
                            {
                                Record = record,
                                Parent = parent
                            };
                            records.Add(record.id, track);
                            parent.Children.Add(record.order, track);
                            processed.Add(record);
                        }
                    }

                    if (processed.Count == 0)
                    {
                        throw new Exception();
                    }

                    foreach (var record in processed)
                    {
                        deferred.Remove(record);
                    }
                }

                var root = records.Single(r => r.Value.Parent == null);
                return root.Value;
            }
        }

        private static async Task<Build> GetBuildData(string url, HttpManager httpManager)
        {
            using (var response = await httpManager.GetAsync(url))
            {
                string json;
                using (var streamReader = new StreamReader(response))
                {
                    json = streamReader.ReadToEnd();
                }
                var buildData = JsonSerializer.Deserialize<Build>(json);
                return buildData;
            }
        }

        [DebuggerDisplay("[{Record.type}] {Record.name} ({Children.Count}) - {Record.result}")]
        private class RecordTracker
        {
            public RecordTracker Parent { get; set; }
            public Timeline.Record Record { get; set; }
            public SortedList<int, RecordTracker> Children { get; } = new SortedList<int, RecordTracker>();
        }

        private class RecordTrackerComparer : IComparer<RecordTracker>
        {
            public int Compare([AllowNull] RecordTracker x, [AllowNull] RecordTracker y)
            {
                return x.Record.order.CompareTo(y.Record.order);
            }

            internal static RecordTrackerComparer Instance { get; } = new RecordTrackerComparer();
        }

        private static async Task<HashSet<string>> FindIncompleteTests(StreamReader stream)
        {
            string lastLine = string.Empty;

            var runningTests = new HashSet<string>();
            string line;
            while ((line = await stream.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lastLine = line;

                    var result = regex.Match(line);
                    if (result.Success)
                    {
                        var status = result.Groups["status"].Value;
                        if (status == "STARTING")
                        {
                            var test = result.Groups["test"].Value.Trim();
                            if (test.Length < 6)
                            {

                            }
                            runningTests.Add(test);
                        }
                        else if (status == "FINISHED")
                        {
                            var test = result.Groups["test"].Value.Trim();
                            if (!runningTests.Remove(test))
                            {
                                Console.WriteLine($"Finished test '{test}' never started");
                            }
                        }
                    }

                }
            }

            if (!lastLine.Contains("##[section]Finishing:"))
            {
                Console.WriteLine($"File does not appear to be a complete build log");
            }

            return runningTests;
        }

        private record BuildInfo
        {
            public uint buildId { get; init; }
            public string buildVersion { get; init; }
            public string url { get; init; }
            public string result { get; init; }
            public DateTime date { get; init; }
            public IReadOnlyCollection<string> issues { get; init; }
        }
    }
}
