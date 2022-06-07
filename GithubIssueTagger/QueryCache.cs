using Octokit;
using System.Collections.Generic;

namespace GithubIssueTagger
{
    internal class QueryCache
    {
        public IReadOnlyList<Label>? AllHomeLabels { get; set; }
        public IReadOnlyList<Issue>? AllHomeIssues { get; set; }
    }
}
