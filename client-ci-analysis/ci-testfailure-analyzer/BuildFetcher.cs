using ci_testfailure_analyzer.Models.AzDO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace ci_testfailure_analyzer
{
    internal class BuildFetcher
    {
        internal static async Task<List<BuildInfo>> DownloadBuildsAsync(DirectoryInfo cache, HttpClient httpClient, int buildDefinition)
        {
            BuildList result = await GetBuildsListAsync(cache, httpClient, buildDefinition);

            // Azure DevOps can mark builds as retained, so they live much longer than the normal retention policy.
            // We have very few builds retained older than 30 days, not enough to make estimates about build
            // reliability during its week/sprint, so let's just ignore them.
            var oldestBuild = DateTime.Today.AddDays(-30);
            List<BuildInfo> builds = result.value
                .Where(b => b.finishTime >= oldestBuild && b.status == "completed" && b.result != "succeeded") // Ignore builds still running
                .OrderBy(b => b.finishTime)
                .ToList();
            return builds;
        }

        private static async Task<BuildList> GetBuildsListAsync(DirectoryInfo cache, HttpClient httpClient, int buildDefinition)
        {
            BuildList result;
            var url = $"https://dev.azure.com/devdiv/devdiv/_apis/build/builds?definitions={buildDefinition}&api-version=5.1&result=!succeeded";
            var jsonCacheFileName = string.Format("builds_{0}_{1:yyyy-MM-dd_hh-mm}.json",
            buildDefinition,
            DateTime.Now);
            var buildsCacheFileInfo = new FileInfo(Path.Combine(cache.FullName, jsonCacheFileName));
            Stopwatch stopwatch = new Stopwatch();

            if (!buildsCacheFileInfo.Exists || buildsCacheFileInfo.LastWriteTimeUtc >= DateTime.UtcNow.AddHours(-1))
            {
                stopwatch.Restart();
                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.Error.Write("Authentication token is expired for https://dev.azure.com/devdiv/devdiv/_apis/build/builds?definitions, please renew!!!!!!!!!!!");
                        Environment.Exit(1);
                    }
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (FileStream file = File.OpenWrite(buildsCacheFileInfo.FullName))
                        {
                            await stream.CopyToAsync(file);
                        }
                        stopwatch.Stop();
                        Console.WriteLine("Got builds after {0}", stopwatch.Elapsed.TotalSeconds);
                    }

                }
            }

            using (var stream = File.OpenRead(buildsCacheFileInfo.FullName))
            {
                // Please note API end point returns only first 1000 builds, currently it's not exceeding 1000 so I didn't make it work more than 1000
                result = await System.Text.Json.JsonSerializer.DeserializeAsync<BuildList>(stream);
            }
            Console.WriteLine("{0} builds in response", result.value.Count);

            return result;
        }

        internal static async Task<List<CsvRow>> GetFailingFunctionalTestAsync(List<BuildInfo> builds, HttpClient httpClient)
        {
            List<TestItem> testFailures = new();
            for (int i = 0; i < builds.Count; i++)
            {
                BuildInfo buildInfo = builds[i];
                string buildId = buildInfo.id.ToString();
                string url = $"https://devdiv.vstmr.visualstudio.com/DevDiv/_apis/testresults/resultsbypipeline?pipelineId={buildId}&outcomes=Failed";     
                Console.WriteLine($"buildId : {buildId} {buildInfo.result}");

                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var testFailure = JsonConvert.DeserializeObject<TestRes>(content);

                        testFailures.AddRange(testFailure.TestItems);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        Console.WriteLine("Response: {0}", response);
                        var content = await response.Content.ReadAsStringAsync();

                        Console.WriteLine("Unexpected error!!!!!!!!!!!");
                        Console.WriteLine(content);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.Error.Write("Authentication token is expired for https://devdiv.vstmr.visualstudio.com/DevDiv/_apis/testresults/resultsbypipeline?pipelineId, please renew!!!!!!!!!!!");
                        Environment.Exit(1);
                    }
                    else
                    {
                        Console.WriteLine("Response: {0}", response);
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Unexpected error!!!!!!!!!!!");
                        Console.WriteLine(content);
                    }

                }
            }

            List<(int testResultId, int testRunId)> failedTestIds = new();
            for (int i = 0; i < testFailures.Count; i++)
            {
                failedTestIds.Add((testFailures[i].Id, testFailures[i].RunId));
            }

            //await GetTestFlakyStatusAsync(failedTestIds, httpClient);
            return await GetAllTestResultsAsync(failedTestIds, httpClient);
        }

        private static async Task<List<CsvRow>> GetAllTestResultsAsync(List<(int, int)> failedTestIds, HttpClient httpClient)
        {
            List<string> results = new();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json;api-version=5.2-preview.1;excludeUrls=true;enumsAsNumbers=true;msDateFormat=true;noArrayWrap=true");

            List<string> tests = new();
            foreach ((int testResultId, int testRunId) failedTest in failedTestIds)
            {
                string test = @"{
                ""id"":" + failedTest.testResultId + @",
                ""testRun"": {
                        ""id"": """ + failedTest.testRunId + @"""
                  }
                }
                ";

                tests.Add(test);

                // There is limit only 200 tests can be quered once.
                if (tests.Count == 200)
                {
                    results.AddRange(await GetBatchedTestResultsAsync(tests, httpClient));

                    tests.Clear();
                }
            }

            if (tests.Count > 0)
            {
                results.AddRange(await GetBatchedTestResultsAsync(tests, httpClient));
            }

            return results.GroupBy(n => n)
                            .Select(c => new CsvRow { TestName = c.Key, Count = c.Count() }).OrderByDescending(r => r.Count).ToList();
        }

        private static async Task<List<string>> GetBatchedTestResultsAsync(List<string> tests, HttpClient httpClient)
        {
            List<string> results = new();
            string testResultsUrl = "https://devdiv.vstmr.visualstudio.com/DevDiv/_apis/testresults/results";

            // Encountered this issue: https://stackoverflow.com/q/10679214
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, testResultsUrl);

            string requestBody = @"{
    ""results"": ["
+ string.Join(",", tests) +
@"],
    ""fields"": [
        ""Outcome"",
        ""TestCaseTitle"",
        ""AutomatedTestName"",
        ""AutomatedTestStorage"",
        ""TestResultGroupType"",
        ""Duration"",
        ""ReleaseEnvId"",
        ""Owner"",
        ""FailingSince"",
        ""DateStarted"",
        ""DateCompleted"",
        ""OutcomeConfidence"",
        ""IsTestResultFlaky"",
        ""TestResultFlakyState"",
        ""CustomFields""
    ]
}";

            request.Content = new StringContent(requestBody,
                                    Encoding.UTF8,
                                    "application/json");

            try
            {
                await httpClient.SendAsync(request)
                  .ContinueWith(async responseTask =>
                  {
                      var res = responseTask.Result;

                      if (res.StatusCode == System.Net.HttpStatusCode.OK)
                      {
                          var content = await res.Content.ReadAsStringAsync();
                          var testResults = JsonConvert.DeserializeObject<TestResults>(content);

                          List<string> allFailedTests = testResults.Results.Select(t => t.TestCaseTitle).ToList();

                          results.AddRange(allFailedTests);
                      }
                  });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return results;
        }

        internal static void WriteCVSFile(List<CsvRow> rows, DirectoryInfo cache, int buildDefinition)
        {
            string cvsFile = string.Format("FailedTestsReport-{0}-{1:yyyy-MM-dd_hh-mm}.csv",
            buildDefinition,
            DateTime.Now);
            cvsFile = Path.Join(cache.FullName, cvsFile);

            using (StreamWriter file = new StreamWriter(cvsFile))
            {
                file.WriteLine("TestFullName, FailureCount");
                for (int i = 0; i < rows.Count; i++)
                {
                    CsvRow row = rows[i];
                    file.WriteLine(StringToCSVCell(row.TestName) + ',' + '"' + row.Count + '"');
                }

                file.WriteLine($"Total test count, {rows.Count}");
                file.WriteLine();
                file.WriteLine($"Failed tests with frequency count from {DateTime.Now.AddDays(-30)} to {DateTime.Now}");

            }

            Console.WriteLine($"Failed test report is written to {cvsFile}");
        }

        /// <summary>
        /// Turn a string into a CSV cell output
        /// </summary>
        /// <param name="str">String to output</param>
        /// <returns>The CSV cell formatted string</returns>
        public static string StringToCSVCell(string str)
        {
            bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }

        // Currently below flaky test detection is not working, it looks it's not enabled for https://dev.azure.com/devdiv/devdiv/, how to enable doc is here https://learn.microsoft.com/en-us/azure/devops/pipelines/test/flaky-test-management?view=azure-devops
        // According to doc it's enabled for whole project, so I don't believe it would be enabled.
        private static async Task GetTestFlakyStatusAsync(List<(int, int)> failedTestIds, HttpClient httpClient)
        {
            foreach ((int testResultId, int testRunId) in failedTestIds)
            {
                //
                string url = $"https://dev.azure.com/devdiv/devdiv/_apis/test/Runs/{testRunId}/results/{testResultId}?api-version=6.1-preview.6";

                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var testFlakinessStatus = JsonConvert.DeserializeObject<TestFlakinessStatus>(content);
                        
                        if (testFlakinessStatus.CustomFields!=null && testFlakinessStatus.CustomFields.Count>0)
                        {
                            Console.Out.WriteLineAsync(string.Join(" ", testFlakinessStatus.CustomFields));
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        Console.WriteLine("Response: {0}", response);
                        var content = await response.Content.ReadAsStringAsync();

                        Console.WriteLine("Unexpected error!!!!!!!!!!!");
                        Console.WriteLine(content);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.Error.Write("Authentication token is expired for https://dev.azure.com/devdiv/devdiv/_apis/test/Runs, please renew!!!!!!!!!!!");
                        Environment.Exit(1);
                    }
                    else
                    {
                        Console.WriteLine("Response: {0}", response);
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Unexpected error!!!!!!!!!!!");
                        Console.WriteLine(content);
                    }

                }
            }
        }
    }
}
