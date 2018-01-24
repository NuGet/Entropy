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

        private static string _buildUrl = Environment.GetEnvironmentVariable("DEVDIV_NUGET_BUILD_URL");

        private const string _branchNameQueryParam = "branchname";
        private const string _commitShaQueryParam = "commit";

        [FunctionName("PREventHandler")]
        public static async Task<object> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "GET")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"NuGet.GithubEventHandler github webhook triggered!");

            try
            {
                var queryParameters = req.GetQueryNameValuePairs();
                var branchName = queryParameters.First(p => string.Equals(p.Key, _branchNameQueryParam, StringComparison.OrdinalIgnoreCase)).Value;
                var commitSha = queryParameters.First(p => string.Equals(p.Key, _commitShaQueryParam, StringComparison.OrdinalIgnoreCase)).Value;

                ValidateBranchName(branchName);

                log.Info($"NuGet.GithubEventHandler queuing a build for branch: {branchName}");

                var res = await QueueBuildAsync(_vstsPat, _devdivBaseUrl, _devdivProjectGuid, _nugetPrivateDefinitionId, branchName, commitSha);
                var response = req.CreateResponse(HttpStatusCode.Redirect);
                var buildUrlWithId = $"{_buildUrl}{res.Id}";
                response.Headers.Add("Location", buildUrlWithId);
                log.Info($"Queued build url: {buildUrlWithId}");
                return response;
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

        private static async Task<Build> QueueBuildAsync(string pat, string url, string project, int buildDefinitionId, string branchName, string commitSha)
        {
            if (string.IsNullOrEmpty(branchName))
            {
                throw new InvalidOperationException("No branch name passed in the request");
            }

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
                SourceBranch = $"refs/heads/{branchName}",
                SourceVersion = commitSha
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