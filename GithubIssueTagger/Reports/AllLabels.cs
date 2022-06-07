using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal class AllLabels : IReport
    {
        private GitHubClient _client;
        private static IReadOnlyList<Label>? _allHomeLabels;

        public AllLabels(GitHubClient client)
        {
            _client = client;
        }

        public async Task RunAsync()
        {
            if (_allHomeLabels is null)
            {
                _allHomeLabels = await LabelUtilities.GetLabelsForRepository(_client, "nuget", "home");
            }
            Console.WriteLine("(ID\tName)");
            foreach (var label in _allHomeLabels)
            {
                Console.WriteLine(label.Id + "\t" + label.Name);
            }
        }
    }
}
