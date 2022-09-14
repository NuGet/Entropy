using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Threading.Tasks;

namespace NuGet.GithubEventHandler
{
    internal class AzDOClient : IAzDOClient
    {
        private IEnvironment _environment;

        public AzDOClient(IEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> QueuePipeline(string org, string project, int pipeline, string gitRef)
        {
            string pat = _environment.Get("AZDO_TOKEN_" + org) ?? string.Empty;
            VssConnection connection = new(new Uri("https://dev.azure.com/" + org), new VssBasicCredential(string.Empty, pat));
            var buildClient = connection.GetClient<BuildHttpClient>();

            var target = new Build()
            {
                Definition = new DefinitionReference()
                {
                    Id = pipeline
                },
                SourceBranch = gitRef
            };

            var result = await buildClient.QueueBuildAsync(target, project);
            var webLink = (ReferenceLink?)result?.Links?.Links["web"];
            return webLink?.Href ?? string.Empty;
        }
    }
}
