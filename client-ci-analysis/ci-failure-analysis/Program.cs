using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ci_failure_analysis
{
    class Program
    {
        static void Main(string[] args)
        {
            var jsonFiles = new[]
            {
                @"private_builds.json",
                @"official_builds.json"
            };

            foreach (var (jsonPath, index) in jsonFiles.Select((item, index) => (item, index)))
            {
                Console.WriteLine(Path.GetFileName(jsonPath));

                var jsonText = File.ReadAllText(jsonPath);
                var builds = System.Text.Json.JsonSerializer.Deserialize<List<BuildInfo>>(jsonText);

                var totalBuilds = builds.Count;

                Console.WriteLine("  Result rates");
                foreach (var group in builds.GroupBy(b=>b.result))
                {
                    var count = group.Count();
                    Console.WriteLine($"    {group.Key} - {count}/{totalBuilds} ({100.0 * count / totalBuilds})");
                }

                var eventuallySuccessful = builds.Where(b => (b.result == "succeeded" || b.result == "partiallySucceeded") && b.jobs.Any(j => j.Value.Count > 1)).ToList();
                Console.WriteLine($"  Succeeded after retry {eventuallySuccessful.Count}");

                var attempts = builds.Where(b => b.jobs?.Count > 0).SelectMany(j => j.jobs.Values).SelectMany(j => j).GroupBy(a => a.result);
                var totalAttempts = attempts.Sum(a => a.Count());
                Console.WriteLine($"  Job attempts");
                Console.WriteLine("    rates");
                foreach (var group in attempts)
                {
                    Console.WriteLine($"      {group.Key} - {group.Count()}/{totalAttempts} ({100.0 * group.Count() / totalAttempts})");
                }

                Console.WriteLine("    job");
                var argh =
                    builds.Where(b => b.jobs?.Count > 0)
                    .SelectMany(b => b.jobs)
                    .SelectMany(b =>
                        b.Value.Select(a => new
                        {
                            Name = b.Key,
                            Successful = a.result == "succeeded"
                        }))
                    .GroupBy(j => j.Name)
                    .Select(j => new
                    {
                        Name = j.Key,
                        Succeeded = j.Count(a => a.Successful),
                        Total = j.Count()
                    })
                    .OrderBy(a => 100.0 * a.Succeeded / a.Total);
                foreach (var job in argh)
                {
                    Console.WriteLine($"      {job.Name} {job.Succeeded}/{job.Total} ({100.0*job.Succeeded/job.Total})");
                }

                Console.WriteLine();
            }
        }

        private static bool IsUserFailure(string reason)
        {
            return reason == "cancelled by user" || reason == "needs work" || reason == "force push before build";
        }

        public class BuildInfo
        {
            public ulong id { get; set; }
            public string buildNumber { get; set; }
            public string url { get; set; }
            public string result { get; set; }
            public DateTime finishTime { get; set; }
            public Dictionary<string, List<Job>> jobs { get; set; }
        }

        public class Job
        {
            public string result { get; set; }
            public string issue { get; set; }
            public string duration { get; set; }
        }
    }
}
