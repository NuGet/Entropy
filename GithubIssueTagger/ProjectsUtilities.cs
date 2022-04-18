using Octokit;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    internal class ProjectsUtilities
    {
        public static async Task RunPlanningAsync(GitHubClient client)
        {
            var projects = await client.Repository.Project.GetAllForOrganization("nuget");

            foreach (var project in projects)
            {
                System.Console.WriteLine($"Name: {project.Name} Url: {project.Url}");
            }
        }
    }
}
