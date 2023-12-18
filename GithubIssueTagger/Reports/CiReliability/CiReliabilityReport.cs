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

        public async Task RunAsync(string sprintName)
        {
            ReportData data = await GetDataAsync(sprintName);

            Output(data);
        }

        private async Task<ReportData> GetDataAsync(string sprintName)
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
| where OrganizationName == 'devdiv' and ProjectId == '0bdbc590-a062-4c3f-b0f6-9383f67865ee' and DefinitionId == 8118 and FinishTime between (start..end) and SourceBranch == 'refs/heads/dev' and Reason == 'schedule';
let sprintBuilds = nugetBuilds
| project BuildId;
let previousAttempts = BuildTimelineRecord
| where OrganizationName == 'devdiv' and ProjectId == '0bdbc590-a062-4c3f-b0f6-9383f67865ee' and BuildId in (sprintBuilds)
| summarize PreviousAttempts=countif(PreviousAttempts !in ('', '[]')) by BuildId
| where PreviousAttempts  > 0
| project BuildId;
nugetBuilds
| where Result !in ('succeeded', 'partiallySucceeded') or BuildId in (previousAttempts)
| order by FinishTime";

            string buildCountQuery = $@"let start = startofday(datetime(""{startOfSprint.ToString("yyyy-MM-dd")}""));
let end = endofday(datetime(""{endOfSprint.ToString("yyyy-MM-dd")}""));
Build
| where OrganizationName == 'devdiv' and ProjectId == '0bdbc590-a062-4c3f-b0f6-9383f67865ee' and DefinitionId == 8118 and FinishTime between (start..end) and SourceBranch == 'refs/heads/dev'
| summarize count()";

            var connectionBuilder = new KustoConnectionStringBuilder("https://1es.kusto.windows.net/", "AzureDevOps")
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
                log?.WriteLine($"Querying builds from {startOfSprint:yyyy-MM-dd} to {endOfSprint:yyyy-MM-dd}");
                var (failedBuilds, trackingIssues) = await GetFailedBuilds(client, crp, failedBuildsQuery, log);

                log?.WriteLine("Querying total builds in sprint");
                int totalBuilds = await GetBuildCount(client, crp, buildCountQuery);

                data = new ReportData()
                {
                    SprintName = sprintName,
                    KustoQuery = failedBuildsQuery,
                    FailedBuilds = failedBuilds,
                    TrackingIssues = trackingIssues,
                    TotalBuilds = totalBuilds
                };
            }

            return data;
        }

        private async Task<int> GetBuildCount(ICslQueryProvider client, ClientRequestProperties crp, string query)
        {
            using var result = await client.ExecuteQueryAsync("AzureDevOps", query, crp);

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
            TextWriter? log)
        {
            List<ReportData.FailedBuild> failedBuilds = new();
            Dictionary<string, string> trackingIssues = new();

            var result = await client.ExecuteQueryAsync("AzureDevOps", query, crp);

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

                var (details, tracking) = await GetFailedBuildDetails(failedBuilds[i].Id, client, crp);

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
            ClientRequestProperties crp)
        {
            List<ReportData.FailureDetail> details = new();
            Dictionary<string, string> trackingIssues = new();

            List<Dictionary<string, object>> rows = new();

            var query = @"BuildTimelineRecord
| where OrganizationName == 'devdiv' and ProjectId == '0bdbc590-a062-4c3f-b0f6-9383f67865ee' and BuildId == " + buildId;
            using (var result = await client.ExecuteQueryAsync("AzureDevOps", query, crp))
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

        private void Output(ReportData data)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (data.FailedBuilds == null) { throw new ArgumentException(paramName: nameof(data.FailedBuilds), message: "data.FailedBuilds must not be null"); }

            float reliability = (data.TotalBuilds - data.FailedBuilds.Count) * 100.0f / data.TotalBuilds;
            int failedBuildsOnlyBecauseOfApex = data.FailedBuilds.Where(b => b.Details?.Count == 1 && b.Details[0].Job == "Apex Test Execution").Count();
            float reliabilityIgnoringApex = (data.TotalBuilds - data.FailedBuilds.Count + failedBuildsOnlyBecauseOfApex) * 100.0f / data.TotalBuilds;

            Console.WriteLine("# NuGet.Client CI Reliability " + data.SprintName);
            Console.WriteLine();
            Console.WriteLine("[NuGet.Client-PR dev branch builds](https://dev.azure.com/devdiv/DevDiv/_build?definitionId=8118&branchFilter=101196%2C101196%2C101196%2C101196%2C101196)");
            Console.WriteLine();
            Console.WriteLine("|Total Builds|Failed Builds|Reliability|Reliability Ignoring Apex|");
            Console.WriteLine("|:--:|:--:|:--:|:--:|");
            Console.WriteLine($"|{data.TotalBuilds}|{data.FailedBuilds.Count}|{reliability:f1}%|{reliabilityIgnoringApex:f1}%|");
            Console.WriteLine();
            Console.WriteLine("## Failed Builds");
            Console.WriteLine();
            Console.WriteLine("**Note:**: Includes builds that succeeded on retry, so first attempt failed");
            Console.WriteLine();
            Console.WriteLine("Kusto ([Needs access to 1ES' CloudMine](https://aka.ms/CloudMine)):");
            Console.WriteLine("```text");
            Console.WriteLine(data.KustoQuery);
            Console.WriteLine("```");
            Console.WriteLine();
            Console.WriteLine("<table>");
            Console.WriteLine("  <tr>");
            Console.WriteLine("    <th>Build</th>");
            Console.WriteLine("    <th>Job</th>");
            Console.WriteLine("    <th>Task</th>");
            Console.WriteLine("    <th>Commentary</th>");
            Console.WriteLine("  </tr>");
            foreach (var build in data.FailedBuilds)
            {
                if (build.Details == null) { throw new ArgumentException(nameof(build.Details), "data.FailedBuilds[int].Details must not be null"); }

                for (int i = 0; i < build.Details.Count; i++)
                {
                    Console.WriteLine("  <tr>");
                    if (i == 0)
                    {
                        if (build.Details.Count > 1)
                        {
                            Console.WriteLine($"    <td rowspan=\"{build.Details.Count}\"><a href=\"https://dev.azure.com/devdiv/DevDiv/_build/results?buildId={build.Id}\">{build.Number}</a></td>");
                        }
                        else
                        {
                            Console.WriteLine($"    <td><a href=\"https://dev.azure.com/devdiv/DevDiv/_build/results?buildId={build.Id}\">{build.Number}</a></td>");
                        }
                    }
                    Console.WriteLine($"    <td>{build.Details[i].Job}</td>");
                    Console.WriteLine($"    <td>{build.Details[i].Task}</td>");
                    Console.WriteLine($"    <td>{build.Details[i].Details}</td>");
                    Console.WriteLine("  </tr>");
                }
            }
            Console.WriteLine("</table>");
            Console.WriteLine();
            Console.WriteLine("### Tracking");

            foreach (var kvp in data.TrackingIssues)
            {
                Console.WriteLine();
                Console.WriteLine($"- {kvp.Key}");
                Console.WriteLine();
                Console.WriteLine(kvp.Value);
            }
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

                command.SetHandler<string>(RunAsync, sprint);

                return command;
            }

            public async Task RunAsync(string sprint)
            {
                var serviceProvider = new ServiceCollection()
                    .AddGithubIssueTagger()
                    .BuildServiceProvider();

                var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (scopeFactory.CreateScope())
                {
                    var report = serviceProvider.GetRequiredService<CiReliabilityReport>();
                    await report.RunAsync(sprint);
                }
            }
        }
    }
}
