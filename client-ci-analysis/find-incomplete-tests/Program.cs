using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace find_incomplete_tests
{
    class Program
    {
        private static Regex regex = new Regex(@"^.*\[xUnit.net [0-9\:\.]*\](?<test>.*)\[(?<status>STARTING|FINISHED)\]");


        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Pass one or more filenames", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
                return;
            }

            foreach (var file in args)
            {
                bool error = false;
                if (!File.Exists(file))
                {
                    Console.WriteLine("File does not exist: " + file);
                    error = true;
                }
                if (error)
                {
                    return;
                }
            }

            var tasks = new List<Task<HashSet<string>>>(args.Length);
            foreach (var file in args)
            {
                tasks.Add(FindIncompleteTests(file));
            }

            await Task.WhenAll(tasks);

            var counts = tasks.SelectMany(t => t.Result)
                .GroupBy(h => h)
                .Select(g => new { Test = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count);

            foreach (var group in counts)
            {
                Console.WriteLine("{0}: {1}", group.Count, group.Test);
            }
        }

        private static async Task<HashSet<string>> FindIncompleteTests(string filename)
        {
            using (var stream = File.OpenText(filename))
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
                    Console.WriteLine($"File '{filename}' does not appear to be a complete build log");
                }

                return runningTests;
            }
        }
    }
}
