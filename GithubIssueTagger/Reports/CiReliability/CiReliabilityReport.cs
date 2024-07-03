using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.CiReliability
{
    [CommandFactory(typeof(CiReliabilityCommandFactory))]
    internal class CiReliabilityReport : IReport
    {
        public Task RunAsync()
        {
            Console.WriteLine("This report cannot run in interactive mode");
            return Task.CompletedTask;
        }

        public async Task RunAsync(string sprintName, string outFile)
        {
            using FileStream fileStream = OpenOutputFile(outFile);

            PipelineData buildPipelineData = new PipelineData()
            {
                PipelineName = "NuGet.Client-PrivateDev",
                BranchFilterQueryString = "101196%2C101196%2C101196%2C101196%2C101196",
                DatabaseName = "AzureDevOps",
                OrganizationName = "devdiv",
                ProjectId = "0bdbc590-a062-4c3f-b0f6-9383f67865ee",
                ProjectName = "DevDiv",
                DefinitionId = "8118",
                SourceBranch = "refs/heads/dev",
                Reason = "schedule",
            };

            ReportData buildReportData = await GetDataAsync(sprintName, queryName: "dev branch builds", buildPipelineData);

            PipelineData funcUnitTestPipelineData = new PipelineData()
            {
                PipelineName = "NuGet.Client CI",
                DatabaseName = "AzureDevOps",
                BranchFilterQueryString = "86197%2C86197%2C86197",
                OrganizationName = "dnceng-public",
                ProjectId = "cbb18261-c48f-4abb-8651-8cdcb5474649",
                ProjectName = "public",
                DefinitionId = "289",
                SourceBranch = "refs/heads/dev",
                Reason = "individualCI",
            };

            ReportData funcUnitTestReportData = await GetDataAsync(sprintName, queryName: "dev branch Functional & Unit tests", funcUnitTestPipelineData);

            var data = new Tuple<PipelineData, ReportData>[] { new(buildPipelineData, buildReportData), new(funcUnitTestPipelineData, funcUnitTestReportData) };
            Output(data, fileStream, reportSprintName: sprintName);
        }

        private FileStream OpenOutputFile(string outFile)
        {
            if (string.IsNullOrWhiteSpace(outFile))
            {
                string error = "Missing required output file (see --output).";
                Console.Error.WriteLine(error);
                throw new ArgumentException(error, paramName: outFile);
            }
            if (File.Exists(outFile))
            {
                string error = $"File already exists: {outFile}";
                Console.Error.WriteLine(error);
                throw new ArgumentException(error, paramName: outFile);
            }

            FileStream fileStream = File.Create(outFile);
            Console.WriteLine($"Created new file: {outFile}");

            return fileStream;
        }

        private async Task<ReportData> GetDataAsync(string sprintName, string queryName, PipelineData pipelineData)
        {
            TextWriter? log;
            if (Console.IsOutputRedirected)
            {
                log = Console.IsErrorRedirected
                    ? null
                    : Console.Error;
            }
            else
            {
                log = Console.Out;
            }

            (DateOnly startOfSprint, DateOnly endOfSprint) = SprintUtilities.GetSprintStartAndEnd(sprintName);

            string failedBuildsQuery = $@"let start = startofday(datetime(""{startOfSprint.ToString("yyyy-MM-dd")}""));
let end = endofday(datetime(""{endOfSprint.ToString("yyyy-MM-dd")}""));
let nugetBuilds = Build
| where OrganizationName == '{pipelineData.OrganizationName}' and ProjectId == '{pipelineData.ProjectId}' and DefinitionId == {pipelineData.DefinitionId} and FinishTime between (start..end) and SourceBranch == '{pipelineData.SourceBranch}' and Reason == '{pipelineData.Reason}';
let sprintBuilds = nugetBuilds
| project BuildId;
let previousAttempts = BuildTimelineRecord
| where OrganizationName == '{pipelineData.OrganizationName}' and ProjectId == '{pipelineData.ProjectId}' and BuildId in (sprintBuilds)
| summarize PreviousAttempts=countif(PreviousAttempts !in ('', '[]')) by BuildId
| where PreviousAttempts  > 0
| project BuildId;
nugetBuilds
| where Result !in ('succeeded', 'partiallySucceeded') or BuildId in (previousAttempts)
| order by FinishTime";

            string buildCountQuery = $@"let start = startofday(datetime(""{startOfSprint.ToString("yyyy-MM-dd")}""));
let end = endofday(datetime(""{endOfSprint.ToString("yyyy-MM-dd")}""));
Build
| where OrganizationName == '{pipelineData.OrganizationName}' and ProjectId == '{pipelineData.ProjectId}' and DefinitionId == {pipelineData.DefinitionId} and FinishTime between (start..end) and SourceBranch == '{pipelineData.SourceBranch}'
| summarize count()";

            var connectionBuilder = new KustoConnectionStringBuilder("https://1es.kusto.windows.net/", pipelineData.DatabaseName)
            {
                FederatedSecurity = true
            };
            ClientRequestProperties crp = new()
            {
                Application = "NuGet Report Generator (https://github.com/NuGet/Entropy/tree/main/GithubIssueTagger)"
            };

            ReportData data;

            using (var client = KustoClientFactory.CreateCslQueryProvider(connectionBuilder))
            {
                log?.WriteLine("Query arguments: pipelineName =" + pipelineData.PipelineName + " | " + "organizationName = " + pipelineData.OrganizationName + " | " + "projectId=" + pipelineData.ProjectId + " | " + "definitionId =" 
                    + pipelineData.DefinitionId + " | " + "sourceBranch=" + pipelineData.SourceBranch + " | " + "reason=" + pipelineData.Reason);
                log?.WriteLine($"Querying builds from {startOfSprint:yyyy-MM-dd} to {endOfSprint:yyyy-MM-dd}");
                var (failedBuilds, trackingIssues) = await GetFailedBuilds(client, crp, failedBuildsQuery, pipelineData, log);

                log?.WriteLine("Querying total builds in sprint");
                int totalBuilds = await GetBuildCount(client, crp, buildCountQuery, pipelineData);

                data = new ReportData()
                {
                    SprintName = sprintName,
                    QueryName = queryName,
                    KustoQuery = failedBuildsQuery,
                    FailedBuilds = failedBuilds,
                    TrackingIssues = trackingIssues,
                    TotalBuilds = totalBuilds
                };
            }

            return data;
        }

        private async Task<int> GetBuildCount(ICslQueryProvider client, ClientRequestProperties crp, string query, PipelineData pipelineData)
        {
            using var result = await client.ExecuteQueryAsync(pipelineData.DatabaseName, query, crp);

            if (!result.Read())
            {
                throw new Exception("Build count query did not return any rows");
            }

            int count;

            checked { count = (int)(long)result[0]; }

            return count;
        }

        private async Task<(IReadOnlyList<ReportData.FailedBuild> failedBuilds, IReadOnlyDictionary<string, string> trackingIssues)> GetFailedBuilds(
            ICslQueryProvider client,
            ClientRequestProperties crp,
            string query,
            PipelineData pipelineData,
            TextWriter? log)
        {
            List<ReportData.FailedBuild> failedBuilds = new();
            Dictionary<string, string> trackingIssues = new();

            var result = await client.ExecuteQueryAsync(pipelineData.DatabaseName, query, crp);

            int buildIdColumn = result.GetOrdinal("BuildId");
            int buildNumberColumn = result.GetOrdinal("BuildNumber");

            while (result.Read())
            {
                long buildId = result.GetInt64(buildIdColumn);
                string buildNumber = result.GetString(buildNumberColumn);

                var failedBuild = new ReportData.FailedBuild()
                {
                    Id = buildId,
                    Number = buildNumber,
                };
                failedBuilds.Add(failedBuild);
            }

            for (int i = 0; i < failedBuilds.Count; i++)
            {
                log?.WriteLine($"Checking failed build {i + 1}/{failedBuilds.Count}");

                var (details, tracking) = await GetFailedBuildDetails(failedBuilds[i].Id, client, crp, pipelineData);

                foreach (var kvp in tracking)
                {
                    if (!trackingIssues.ContainsKey(kvp.Key))
                    {
                        trackingIssues.Add(kvp.Key, kvp.Value);
                    }
                }

                failedBuilds[i] = failedBuilds[i] with
                {
                    Details = details,
                };
            }

            return (failedBuilds, trackingIssues);
        }

        private async Task<(IReadOnlyList<ReportData.FailureDetail> details, IReadOnlyDictionary<string, string> tracking)> GetFailedBuildDetails(
            long buildId,
            ICslQueryProvider client,
            ClientRequestProperties crp,
            PipelineData pipelineData)
        {
            List<ReportData.FailureDetail> details = new();
            Dictionary<string, string> trackingIssues = new();

            List<Dictionary<string, object>> rows = new();

            var query = $@"BuildTimelineRecord
| where OrganizationName == '{pipelineData.OrganizationName}' and ProjectId == '{pipelineData.ProjectId}' and BuildId == {buildId}";
            using (var result = await client.ExecuteQueryAsync(pipelineData.DatabaseName, query, crp))
            {
                while (result.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < result.FieldCount; i++)
                    {
                        var name = result.GetName(i);
                        var value = result.GetValue(i);
                        row[name] = value;
                    }

                    rows.Add(row);
                }
            }

            if (rows.Count == 0)
            {
                // Pipeline failed to run. Maybe yaml error?
                ReportData.FailureDetail detail = new()
                {
                    Job = "Pipeline",
                    Task = "",
                    Details = ""
                };
                details.Add(detail);
                return (details, trackingIssues);
            }

            foreach (var row in rows.Where(IsFailedJob))
            {
                string job = (string)row["RecordName"];
                var jobId = (string)row["RecordId"];
                string task =
                    (string?)rows
                    .Where(row => IsFailedTask(row, jobId))
                    .OrderBy(r => r["Order"])
                    .FirstOrDefault()
                    ?["RecordName"] ?? string.Empty;

                string reason = string.Empty;
                // if (job == "Apex Test Execution" && task == "Run Tests")
                // {
                //     reason = "Apex jobs not investigated due to high failure count";
                //     trackingIssues.Add("Apex flakiness",
                //         "https://github.com/NuGet/Client.Engineering/issues/1299");
                // }

                ReportData.FailureDetail detail = new()
                {
                    Job = job,
                    Task = task,
                    Details = reason
                };
                details.Add(detail);
            }

            Debug.Assert(details.Count > 0);

            return (details, trackingIssues);

            bool IsFailedJob(Dictionary<string, object> timelineEntry)
            {
                if ((string)timelineEntry["Type"] != "Job")
                {
                    return false;
                }

                string result = (string)timelineEntry["Result"];
                if (result == "succeeded" || result == "partiallySucceeded")
                {
                    string previousAttempts = (string)timelineEntry["PreviousAttempts"];
                    if (string.IsNullOrEmpty(previousAttempts) || previousAttempts == "[]")
                    {
                        return false;
                    }
                }

                return true;
            }

            bool IsFailedTask(Dictionary<string, object> timelineEntry, string parentId)
            {
                string currentParentId = (string)timelineEntry["ParentId"];
                if (currentParentId != parentId)
                {
                    return false;
                }

                var result = (string)timelineEntry["Result"];
                return result != "succeeded" && result != "succeededWithIssues" && result != "skipped";
            }
        }

        private void Output(Tuple<PipelineData, ReportData>[] pipelineReportDataList, FileStream outputFileStream, string reportSprintName)
        {
            if (outputFileStream is null || !outputFileStream.CanRead || !outputFileStream.CanWrite) { throw new ArgumentException(paramName: nameof(outputFileStream), message: "Cannot read and write to output file"); }
            using var sw = new StreamWriter(outputFileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            sw.WriteLine($"# NuGet Client CI Reliability " + reportSprintName);

            foreach (var pipelineReportData in pipelineReportDataList)
            {
                var pipelineData = pipelineReportData.Item1;
                var data = pipelineReportData.Item2;
                if (data == null) { throw new ArgumentNullException(nameof(data)); }
                if (data.FailedBuilds == null) { throw new ArgumentException(paramName: nameof(data.FailedBuilds), message: "data.FailedBuilds must not be null"); }

                float reliability = (data.TotalBuilds - data.FailedBuilds.Count) * 100.0f / data.TotalBuilds;
                int failedBuildsOnlyBecauseOfApex = data.FailedBuilds.Where(b => b.Details?.Count == 1 && b.Details[0].Job == "Apex Test Execution").Count();
                float reliabilityIgnoringApex = (data.TotalBuilds - data.FailedBuilds.Count + failedBuildsOnlyBecauseOfApex) * 100.0f / data.TotalBuilds;

                OutputPipelineData(pipelineData, data, reliability, reliabilityIgnoringApex, sw);

                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine("### Tracking");
                if (data.TrackingIssues.Count == 0)
                {
                    sw.WriteLine("No tracking issues");
                }
                else
                {
                    foreach (var kvp in data.TrackingIssues)
                    {
                        sw.WriteLine();
                        sw.WriteLine($"- {kvp.Key}");
                        sw.WriteLine();
                        sw.WriteLine(kvp.Value);
                    }
                }
            }
        }

        private static void OutputPipelineData(PipelineData pipelineData, ReportData data, float reliability, float reliabilityIgnoringApex, StreamWriter sw)
        {
            sw.WriteLine($"## {pipelineData.PipelineName}");
            sw.WriteLine();
            sw.WriteLine($"[{pipelineData.PipelineName} {data.QueryName}](https://dev.azure.com/{pipelineData.OrganizationName}/{pipelineData.ProjectName}/_build?definitionId={pipelineData.DefinitionId}&branchFilter={pipelineData.BranchFilterQueryString})");
            sw.WriteLine();
            sw.WriteLine("|Total Builds|Failed Builds|Reliability|Reliability Ignoring Apex|");
            sw.WriteLine("|:--:|:--:|:--:|:--:|");
            sw.WriteLine($"|{data.TotalBuilds}|{data.FailedBuilds.Count}|{reliability:f1}%|{reliabilityIgnoringApex:f1}%|");
            sw.WriteLine();
            sw.WriteLine("### Failed Builds");
            sw.WriteLine();
            sw.WriteLine("**Note:**: Includes builds that succeeded on retry, so first attempt failed");
            sw.WriteLine();
            sw.WriteLine("Kusto ([Needs access to 1ES' CloudMine](https://aka.ms/CloudMine)):");
            sw.WriteLine("```text");
            sw.WriteLine(data.KustoQuery);
            sw.WriteLine("```");
            sw.WriteLine();
            sw.WriteLine("<table>");
            sw.WriteLine("  <tr>");
            sw.WriteLine("    <th>Build</th>");
            sw.WriteLine("    <th>Job</th>");
            sw.WriteLine("    <th>Task</th>");
            sw.WriteLine("    <th>Commentary</th>");
            sw.WriteLine("  </tr>");
            foreach (var build in data.FailedBuilds)
            {
                if (build.Details == null) { throw new ArgumentException(nameof(build.Details), "data.FailedBuilds[int].Details must not be null"); }

                for (int i = 0; i < build.Details.Count; i++)
                {
                    sw.WriteLine("  <tr>");
                    if (i == 0)
                    {
                        if (build.Details.Count > 1)
                        {
                            sw.WriteLine($"    <td rowspan=\"{build.Details.Count}\"><a href=\"https://dev.azure.com/{pipelineData.OrganizationName}/{pipelineData.ProjectName}/_build/results?buildId={build.Id}\">{build.Number}</a></td>");
                        }
                        else
                        {
                            sw.WriteLine($"    <td><a href=\"https://dev.azure.com/{pipelineData.OrganizationName}/{pipelineData.ProjectName}/_build/results?buildId={build.Id}\">{build.Number}</a></td>");
                        }
                    }
                    sw.WriteLine($"    <td>{build.Details[i].Job}</td>");
                    sw.WriteLine($"    <td>{build.Details[i].Task}</td>");
                    sw.WriteLine($"    <td>{build.Details[i].Details}</td>");
                    sw.WriteLine("  </tr>");
                }
            }
            sw.WriteLine("</table>");
        }

        private class CiReliabilityCommandFactory : ICommandFactory
        {
            public Command CreateCommand(Type type, GitHubPatBinder patBinder)
            {
                var command = new Command(nameof(CiReliability));
                command.Description = "Find NuGet.Client build reliability and build failures.";

                var sprint = new Option<string>("--sprint");
                sprint.AddAlias("-s");
                sprint.Description = "Sprint name";
                sprint.IsRequired = true;
                command.AddOption(sprint);

                var outFile = new Option<string>("--output");
                outFile.AddAlias("-o");
                outFile.IsRequired = true;
                outFile.Description = "Output path and file name (.md)";
                command.AddOption(outFile);

                command.SetHandler<string, string>(RunAsync, sprint, outFile);

                return command;
            }

            public async Task RunAsync(string sprint, string outFile)
            {
                var serviceProvider = new ServiceCollection()
                    .AddGithubIssueTagger()
                    .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (scopeFactory.CreateScope())
                {
                    var report = serviceProvider.GetRequiredService<CiReliabilityReport>();
                    await report.RunAsync(sprint, outFile);
                }
            }
        }
    }
}
