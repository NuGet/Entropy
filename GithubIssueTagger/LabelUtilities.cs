using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public class LabelUtilities
    {
        /// <summary>
        /// Run this to align the labels between the NuGet/Home & NuGet/Client.Engineering repos
        /// </summary>
        public static async Task AlignNuGetClientRepoLabels(GitHubClient client)
        {
            await AlignRepositoryLabels(client, "nuget", "home", "nuget", "client.engineering",
                                        excludeList: new List<string>() { "Triage:*", "cla-*", "Epic", "UpFor", "Waiting", "Infrastructure" },
                                        retainList: new List<string>() { "Engineering Excellence", "CI initiative", "Epic", "HotSeat", "Infrastructure" });
        }

        /// <summary>
        /// Align the issues between the two repos in question. 
        /// </summary>
        /// <param name="client">Github client with sufficient permissions.</param>
        /// <param name="fromOrg">The org that contains the labels to copy from.</param>
        /// <param name="fromRepo">The repo that contains the labels to copy from.</param>
        /// <param name="toOrg">The org that contains the labels to copy to.</param>
        /// <param name="toRepo">The repo that contains the labels to copy to.</param>
        /// <param name="excludeList">List of labels that shouldn't be copied.</param>
        /// <param name="retainList">List of labels from the to repo that should be retain, despite the fact that they might not exist in the from repo.</param>
        /// <returns></returns>
        public static async Task AlignRepositoryLabels(GitHubClient client, string fromOrg, string fromRepo, string toOrg, string toRepo, IList<string> excludeList, IList<string> retainList)
        {
            var fromLabels = await GetLabelsForRepository(client, fromOrg, fromRepo);
            var toLabels = await GetLabelsForRepository(client, toOrg, toRepo);
            var filteredFromLabels = Filter(fromLabels, excludeList);
            var filteredToLabels = Filter(toLabels, retainList);

            // 1st Pass. Update all the labels.
            var toUpdate = filteredFromLabels.Where(e => filteredToLabels.Any(toLabel => toLabel.Name.Equals(e.Name))).ToList();
            Console.WriteLine($"Ensuring {toUpdate.Count} labels are up to date.");
            foreach (var label in toUpdate)
            {
                if(await EnsureLabelUpToDate(client, label, toOrg, toRepo))
                {
                    Console.WriteLine($"{label.Name} was updated in {toOrg}/{toRepo}");
                }
            }
            Console.WriteLine();

            // 2nd Pass. Create all the new labels
            var toCreate = filteredFromLabels.Where(fromLabel => !filteredToLabels.Any(toLabel => toLabel.Name.Equals(fromLabel.Name))).ToList();
            Console.WriteLine($"Creating {toCreate.Count} labels!");
            foreach (var label in toCreate)
            {
                await CreateLabel(client, label, toOrg, toRepo);
                Console.WriteLine($"{label.Name} was created in {toOrg}/{toRepo}");
            }

            // 3rd pass remove labels that don't match. 
            var toRemove = filteredToLabels.Where(e => !filteredFromLabels.Any(fromLabel => fromLabel.Name.Equals(e.Name))).ToList();
            foreach (var label in toRemove)
            {
                await RemoveLabel(client, label, toOrg, toRepo);
                Console.WriteLine($"{label.Name} was removed in {toOrg}/{toRepo}");
            }
        }

        private static void PrintLabels(IEnumerable<Label> filteredFromLabels)
        {
            Console.WriteLine();
            foreach (var label in filteredFromLabels)
            {
                Console.WriteLine(label.Name);
            }
            Console.WriteLine();
        }

        public static async Task CreateLabel(GitHubClient client, Label fromLabel, string toOrg, string toRepo)
        {
            var newLabel = new NewLabel(fromLabel.Name, fromLabel.Color)
            {
                Description = fromLabel.Description
            };

            await client.Issue.Labels.Create(toOrg, toRepo, newLabel);
        }

        public static async Task RemoveLabel(GitHubClient client, Label fromLabel, string toOrg, string toRepo)
        {
            await client.Issue.Labels.Delete(toOrg, toRepo, fromLabel.Name);
        }

        public static async Task<bool> EnsureLabelUpToDate(GitHubClient client, Label fromLabel, string toOrg, string toRepo)
        {
            var label = await client.Issue.Labels.Get(toOrg, toRepo, fromLabel.Name);

            // If the labels differs even a bit, update it!
            if (!(label.Name.Equals(fromLabel.Name) &&
                label.Description.Equals(fromLabel.Description) &&
                label.Color.Equals(fromLabel.Color)))
            {
                var newLabel = new LabelUpdate(fromLabel.Name, fromLabel.Color)
                {
                    Description = fromLabel.Description
                };

                await client.Issue.Labels.Update(toOrg, toRepo, fromLabel.Name, newLabel);
                return true;
            }
            return false;
        }

        private static List<Label> Filter(IReadOnlyList<Label> labels, IList<string> regexList)
        {
            var relevant = new List<Label>();

            foreach (var label in labels)
            {
                if (!regexList.Select(e => new Regex(e)).Any(regex => regex.IsMatch(label.Name)))
                {
                    relevant.Add(label);
                }
            }

            return relevant;
        }

        public static async Task<IReadOnlyList<Label>> GetLabelsForRepository(GitHubClient client, string fromOrg, string fromRepo)
        {
            var repoId = await client.Repository.Get(fromOrg, fromRepo);
            return await GetAllLabels(client, repoId);
        }

        private static async Task<IReadOnlyList<Label>> GetAllLabels(GitHubClient client, Repository repoId)
        {
            int page = 0;
            int totalProcessed = 0;
            int totalExpected = 1;
            List<Label> labels = new List<Label>();
            while (totalProcessed < totalExpected)
            {
                page++;

                var request = new SearchLabelsRequest("\"\"", repoId.Id)
                {
                    RepositoryId = repoId.Id,
                    Page = page
                };
                var result = await client.Search.SearchLabels(request);
                totalExpected = result.TotalCount;

                foreach (var label in result.Items)
                {
                    labels.Add(label);
                }
                totalProcessed += result.Items.Count;
            }
            return labels.OrderBy(e => e.Name).ToList();
        }
    }
}
