using System.Threading.Tasks;

namespace NuGet.GithubEventHandler
{
    public interface IAzDOClient
    {
        /// <summary>Queue a pipeline</summary>
        /// <param name="org">The Azure DevOps Organization account</param>
        /// <param name="project">The Azure DevOps Project</param>
        /// <param name="pipeline">The Azure Pipeline definition ID.</param>
        /// <param name="gitRef">The git reference. For example, it could be a commit hash, a branch or tag name, or a GitHub pull request ref.</param>
        /// <returns>URL to view the queued build in a browser.</returns>
        Task<string> QueuePipeline(string org, string project, int pipeline, string gitRef);
    }
}
