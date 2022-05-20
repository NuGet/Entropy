using Octokit;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace GithubIssueTagger.Reports
{
    internal class AreaLabels : IReport
    {
        private GitHubClient _client;
        private QueryCache _queryCache;

        public AreaLabels(GitHubClient client, QueryCache queryCache)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        }

        public async Task Run()
        {
            IEnumerable<Label> areaLabels = await GetAreaLabels();
            Console.WriteLine("(ID\tName)");
            foreach (var label in areaLabels)
            {
                Console.WriteLine(label.Id + "\t" + label.Name);
            }
        }

        private async Task<IEnumerable<Label>> GetAreaLabels()
        {
            if (_queryCache.AllHomeLabels is null)
            {
                _queryCache.AllHomeLabels = await LabelUtilities.GetLabelsForRepository(_client, "nuget", "home");
            }

            IEnumerable<Label> areaLabels = _queryCache.AllHomeLabels?.Where(l => l.Name.StartsWith("Area:", StringComparison.OrdinalIgnoreCase));
            return areaLabels;
        }
    }
}
