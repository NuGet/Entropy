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

            var buildsCacheFileInfo = new FileInfo(Path.Combine(cache.FullName, $"builds_{buildDefinition}.json"));
            Stopwatch stopwatch = new Stopwatch();
            if (!buildsCacheFileInfo.Exists || buildsCacheFileInfo.LastWriteTimeUtc >= DateTime.UtcNow.AddHours(-1))
            {
                stopwatch.Restart();
                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("Response: {0}", response);
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Authentication token is expired, please renew!!!!!!!!!!!");
                        Console.WriteLine(content);
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
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "api-version=5.0-preview.1;excludeUrls=true;enumsAsNumbers=true;msDateFormat=true;noArrayWrap=true");

            List<TestFailure> testFailures = new();
            for (int i = 0; i < builds.Count; i++)
            {
                BuildInfo buildInfo = builds[i];
                string url = "https://devdiv.visualstudio.com/_apis/Contribution/HierarchyQuery/project/0bdbc590-a062-4c3f-b0f6-9383f67865ee";
                string buildId = buildInfo.id.ToString();
                // Encountered this issue: https://stackoverflow.com/q/10679214
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                string requestBody = "{\"contributionIds\":[\"ms.vss-test-web.test-tab-unifiedPipeline-summary-data-provider\"],\"dataProviderContext\":{\"properties\":{\"sourcePage\":{\"url\":\"https://devdiv.visualstudio.com/DevDiv/_build/results?buildId=" + buildId + "&view=ms.vss-test-web.build-test-results-tab\",\"routeValues\":{\"project\":\"DevDiv\",\"viewname\":\"build-results\"}}}}}";

                request.Content = new StringContent(requestBody,
                                        Encoding.UTF8,
                                        "application/json");

                Console.WriteLine($"buildId : {buildId} {buildInfo.result}");

                await httpClient.SendAsync(request)
                  .ContinueWith(async responseTask =>
                  {
                      var res = responseTask.Result;

                      if (res.StatusCode == System.Net.HttpStatusCode.OK)
                      {
                          var content = await res.Content.ReadAsStringAsync();
                          TestFailure testFailure = JsonConvert.DeserializeObject<Models.AzDO.TestFailure>(content);

                          if (testFailure?.dataProviders?.MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider?.resultsAnalysis != null)
                          {
                              testFailures.Add(testFailure);
                          }
                          else
                          {
                              // This build doesn't have actual test error, most likely build error.
                          }
                      }
                      else if (res.StatusCode == System.Net.HttpStatusCode.BadRequest)
                      {
                          Console.WriteLine("Response: {0}", res);
                          var content = await res.Content.ReadAsStringAsync();

                          Console.WriteLine("Unexpected error!!!!!!!!!!!");
                          Console.WriteLine(content);
                      }
                      else if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                      {
                          Console.WriteLine("Response: {0}", res);
                          var content = await res.Content.ReadAsStringAsync();
                          Console.WriteLine("Authentication token is expired, please renew!!!!!!!!!!!");
                          Console.WriteLine(content);
                          Environment.Exit(1);
                      }
                      else
                      {
                          Console.WriteLine("Response: {0}", res);
                          var content = await res.Content.ReadAsStringAsync();
                          Console.WriteLine("Unexpected error!!!!!!!!!!!");
                          Console.WriteLine(content);
                      }
                  });
            }

            List<(int testResultId, int testRunId, int pipelineId)> failedTestIds = new();
            for (int i = 0; i < testFailures.Count; i++)
            {
                failedTestIds.AddRange(testFailures[i].dataProviders.MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider.resultsAnalysis.testFailuresAnalysis.newFailures.testResults.Select(x => (x.testResultId, x.testRunId, testFailures[i].dataProviders.MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider.currentContext.pipelineId)));
                failedTestIds.AddRange(testFailures[i].dataProviders.MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider.resultsAnalysis.testFailuresAnalysis.existingFailures.testResults.Select(x => (x.testResultId, x.testRunId, testFailures[i].dataProviders.MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider.currentContext.pipelineId)));
            }

            return await GetAllTestResultsAsync(failedTestIds, httpClient);
        }

        private static async Task<List<CsvRow>> GetAllTestResultsAsync(List<(int, int, int)> failedTestIds, HttpClient httpClient)
        {
            List<Result> results = new();
            HashSet<int> protocolTestBuild = new();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json;api-version=5.2-preview.1;excludeUrls=true;enumsAsNumbers=true;msDateFormat=true;noArrayWrap=true");

            List<string> tests = new();
            foreach ((int testResultId, int testRunId, int pipelineId) failedTest in failedTestIds)
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
                if (tests.Count == 1)
                {
                    var testDetails = await GetBatchedTestResultsAsync(tests, httpClient);
                    results.AddRange(testDetails);
                    foreach (Result testDetail in testDetails)
                    {
                        if (testDetail.AutomatedTestName == "NuGet.Protocol.Tests.ServiceIndexResourceV3ProviderTests.Query_For_Resource_ReturnAllOfSameTypeVersion")
                        {
                            protocolTestBuild.Add(failedTest.pipelineId);
                        }
                    }
                    tests.Clear();
                }
            }

            if (tests.Count > 0)
            {
                results.AddRange(await GetBatchedTestResultsAsync(tests, httpClient));
            }

            return results.Select(t => t.AutomatedTestName).GroupBy(n => n)
                            .Select(c => new CsvRow { TestName = c.Key, Count = c.Count() }).OrderByDescending(r => r.Count).ToList();
        }

        private static async Task<List<Result>> GetBatchedTestResultsAsync(List<string> tests, HttpClient httpClient)
        {
            List<Result> results = new();
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
        ""TestResultFlakyState""
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
                          TestResults testResults = JsonConvert.DeserializeObject<Models.AzDO.TestResults>(content);

                          results.AddRange(testResults.Results);
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
            string cvsFile = string.Format("failedTestsReport-{0}-{1:yyyy-MM-dd_hh-mm}.csv",
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
    }
}
