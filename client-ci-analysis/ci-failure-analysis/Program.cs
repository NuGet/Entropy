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
                var totalFailures = builds.SelectMany(b => b.issues).Count();
                var infrastructureFailures = builds.Where(b => b.issues.Any(r => !IsUserFailure(r))).SelectMany(b => b.issues).Count();

                bool foundFailedBuildMissingReason = false;
                foreach (var failedBuildsMissingReason in builds.Where(b=> b.result == "failed" && b.issues.Count == 0))
                {
                    foundFailedBuildMissingReason = true;
                    Console.WriteLine($"Warning: build {failedBuildsMissingReason.buildId} does not have a failed reason");
                }
                if (foundFailedBuildMissingReason)
                {
                    Console.WriteLine();
                }

                Console.WriteLine("Failed builds: " + totalBuilds);
                Console.WriteLine("Failure reasons: " + totalFailures);
                Console.WriteLine("Infrastructure failures: " + infrastructureFailures);
                Console.WriteLine();

                var buildsByFailures = builds.SelectMany(b => b.issues.Select(r => new { Build = b.issues, Reason = r })).GroupBy(b => b.Reason).ToDictionary(b => b.Key, b => b.Select(r => r.Build).ToList());

                foreach (var failure in buildsByFailures.OrderByDescending(f => f.Value.Count))
                {
                    int count = failure.Value.Count;
                    int failureRate = 100 * count / totalFailures;
                    int? infraFailureRate = IsUserFailure(failure.Key)
                        ? (int?)null
                        : 100 * count / infrastructureFailures;

                    Console.WriteLine($"{failure.Key}: {failure.Value.Count} {failureRate}% {infraFailureRate?.ToString() ?? "-"}%");
                }

                var macHangs = builds.Where(b => b.issues.Contains("mac tests hung")).ToList();
                if (macHangs.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Mac test hangs:");
                    Console.WriteLine("|Date|Build|Agent|");
                    Console.WriteLine("|----|----|----|");

                    foreach (var b in macHangs)
                    {
                        Console.WriteLine($"|{b.date:yyyy-MM-dd}|[{b.buildVersion}](https://dev.azure.com/devdiv/DevDiv/_build/results?buildId={b.buildId}&view=logs&j=a1762d43-9ec6-55e8-af8b-c0d9842bd83b&t=df101a74-a91b-5951-ad8b-3bf8c87bc347)||");
                    }
                }

                var linuxHangs = builds.Where(b => b.issues.Contains("linux tests hung")).ToList();
                if (linuxHangs.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("linux test hangs:");
                    Console.WriteLine("|Date|Build|Agent|");
                    Console.WriteLine("|----|----|----|");

                    foreach (var b in linuxHangs)
                    {
                        Console.WriteLine($"|{b.date:yyyy-MM-dd}|[{b.buildVersion}](https://dev.azure.com/devdiv/DevDiv/_build/results?buildId={b.buildId}&view=logs&j=bd7d45da-bd9f-59a9-6834-84d44942bc5e&t=9a50982a-6f1b-5916-658b-78b1ed438169)||");
                    }
                }


                if (index != jsonFiles.Length - 1)
                {
                    Console.WriteLine();
                }
            }
        }

        private static bool IsUserFailure(string reason)
        {
            return reason == "cancelled by user" || reason == "needs work" || reason == "force push before build";
        }

        public class BuildInfo
        {
            public ulong buildId { get; set; }
            public string buildVersion { get; set; }
            public string url { get; set; }
            public string result { get; set; }
            public DateTime date { get; set; }
            public List<string> issues { get; set; }
        }
    }
}
