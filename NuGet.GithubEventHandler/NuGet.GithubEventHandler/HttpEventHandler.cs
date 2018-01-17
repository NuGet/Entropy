// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Newtonsoft.Json.Linq;

namespace NuGet.GithubEventHandler
{
    // References - 
    // vsts build api - https://www.visualstudio.com/en-us/docs/integrate/api/build/builds#queue-a-build
    // vsts managed code - http://tech.en.tanaka733.net/entry/queue-and-cancel-vsts-build-from-csharp

    public static class HttpEventHandler
    {
        private static string _devdivBaseUrl = Environment.GetEnvironmentVariable("DEVDIV_VSTS_BASE_URL");
        private static string _devdivProjectGuid = Environment.GetEnvironmentVariable("DEVDIV_VSTS_PROJECT_GUID");
        private static int _nugetPrivateDefinitionId = Int32.Parse(Environment.GetEnvironmentVariable("NUGET_VSTS_PRIVATE_BUILD_DEFINITION_ID"));
        private static string _vstsPat = Environment.GetEnvironmentVariable("VSTS_PAT_ENV_VAR");

        [FunctionName("PREventHandler")]
        public static async Task<object> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "POST", WebHookType = "github")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"NuGet.GithubEventHandler github webhook triggered!");

            try
            {
                var jsonContent = await req.Content.ReadAsStringAsync();
                var data = JObject.Parse(jsonContent);
                var action = GetProperty<string>(data, "action");

                if (!string.Equals(action, "labeled", StringComparison.OrdinalIgnoreCase))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        error = "This function only works on applying labels in a PR"
                    });
                }

                var label = GetProperty<JObject>(data, "label");
                var labelName = GetProperty<string>(label, "name");

                if (!string.Equals(labelName, "Build On CI", StringComparison.OrdinalIgnoreCase))
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        error = "This function only works on applying 'Build On CI' label to a PR"
                    });
                }

                var pr = GetProperty<JObject>(data, "pull_request");
                var head = GetProperty<JObject>(pr, "head");
                var branchName = GetProperty<string>(head, "ref");

                log.Info($"NuGet.GithubEventHandler github webhook queuing a build for branch: {branchName}");

                ValidateBranchName(branchName);

                var res = await QueueBuildAsync(_vstsPat, _devdivBaseUrl, _devdivProjectGuid, _nugetPrivateDefinitionId, branchName);

                return req.CreateResponse(HttpStatusCode.OK, new
                {
                    greeting = $"Action: {action}, branch: {branchName}, build: {res.Id}"
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void ValidateBranchName(string branchName)
        {
            if (!branchName.StartsWith("dev-", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"This function only works on branch names starting with dev-");
            }
        }

        private static async Task<Build> QueueBuildAsync(string pat, string url, string project, int buildDefinitionId, string branchName)
        {
            if (string.IsNullOrEmpty(pat))
            {
                throw new InvalidOperationException("Unable to read PAT for vsts");
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException("Unable to read DevDiv vsts base url");
            }

            if (string.IsNullOrEmpty(project))
            {
                throw new InvalidOperationException("Unable to read DevDiv vsts project guid");
            }

            var build = new BuildHttpClient(new Uri(url), new VssBasicCredential(string.Empty, _vstsPat));

            // Get the list of build definitions.
            var definitions = await build.GetDefinitionsAsync(project: project);

            // Find the private dev definition
            var target = definitions.First(d => d.Id == buildDefinitionId);

            return await build.QueueBuildAsync(new Build
            {
                Definition = new DefinitionReference
                {
                    Id = target.Id
                },
                Project = target.Project,
                Priority = QueuePriority.Normal,
                Reason = BuildReason.PullRequest,
                SourceBranch = $"refs/heads/{branchName}"
            });
        }

        private static T GetProperty<T>(JObject json, string propertyName)
        {
            var property = json[propertyName].Value<T>();

            if (property == null)
            {
                throw new InvalidDataException($"Property '{propertyName}' not present in the json");
            }

            return property;
        }
    }
}