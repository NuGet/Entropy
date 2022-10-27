using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#nullable disable

namespace GithubIssueTagger
{
    public static class PlanningUtilities
    {
        private static readonly string SourceMappingLabel = "Area:PackageSourceMapping";
        private static readonly string SeasonOfGiving = "Category:SeasonOfGiving";
        private static readonly string TypeFeatureLabel = "Type:Feature";
        private static readonly string TypeSpec = "Type:Spec";

        public static async Task RunPlanningAsync(GitHubClient client)
        {
            IEnumerable<Issue> issues = await GetIssuesForLabelFromBothClientRepos(client, SeasonOfGiving);

            var markdownTable = issues.Select(e => new IssueModel(e)).ToMarkdownTable(GetSeasonOfGivingColumns());

            Console.WriteLine();
            Console.WriteLine(markdownTable);
            Console.WriteLine();
            Console.ReadKey();
        }

        public static async Task<IEnumerable<Issue>> GetPackageSourceMappingFeatureIssues(GitHubClient client)
        {
            var homeIssues = await IssueUtilities.GetIssuesForLabelsAsync(client, "nuget", "home", SourceMappingLabel, TypeFeatureLabel);
            var clientEngineeringIssues = await IssueUtilities.GetIssuesForLabelsAsync(client, "nuget", "client.engineering", SourceMappingLabel, TypeFeatureLabel);
            var issues = (homeIssues.Union(clientEngineeringIssues)).ToList();

            return issues;
        }

        public static async Task<IEnumerable<Issue>> GetPackageSourceMappingDesign(GitHubClient client)
        {
            var homeIssues = await IssueUtilities.GetIssuesForLabelsAsync(client, "nuget", "home", SourceMappingLabel, TypeSpec);
            var clientEngineeringIssues = await IssueUtilities.GetIssuesForLabelsAsync(client, "nuget", "client.engineering", SourceMappingLabel, TypeSpec);
            var issues = (homeIssues.Union(clientEngineeringIssues)).ToList();

            return issues;
        }

        public static async Task<IEnumerable<Issue>> GetPackageSourceMappingIssuesForSprint(GitHubClient client)
        {
            static bool isRelevant(Issue x) => IsPackageSourceMappingIssue(x);
            var clientEngineeringIssues = await IssueUtilities.GetIssuesForMilestoneAsync(client, "nuget", "client.engineering", "34", isRelevant);
            // 2021-11 is 131 in Home and 2021-11 is 34 on Client.Engineering
            var homeIssues = await IssueUtilities.GetIssuesForMilestoneAsync(client, "nuget", "home", "131", isRelevant);
            var issues = (homeIssues.Union(clientEngineeringIssues)).ToList();

            return issues;
        }

        public static async Task<IList<Issue>> GetIssuesForLabelFromBothClientRepos(GitHubClient client, string label)
        {
            var homeIssues = await IssueUtilities.GetIssuesForLabelAsync(client, "nuget", "home", label);
            var clientEngineeringIssues = await IssueUtilities.GetIssuesForLabelAsync(client, "nuget", "client.engineering", label);
            var issues = (homeIssues.Union(clientEngineeringIssues)).OrderBy(e => e.Repository).OrderBy(e => e.Number).ToList();

            return issues;
        
        }

        public static List<Tuple<string, string>> GetPackageSourceMapping()
        {
            return new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("Link", "Link"),
                    new Tuple<string, string>("Title", "Title"),
                    new Tuple<string, string>("Assignee", "Assignee"),
                    new Tuple<string, string>("Milestone", "Milestone"),
                };
        }

        public static List<Tuple<string, string>> GetSeasonOfGivingColumns()
        {
            return new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("Link", "Link"),
                    new Tuple<string, string>("Title", "Title"),
                    new Tuple<string, string>("Cost", "Cost"),
                    new Tuple<string, string>("Assignee", "Assignee"),
                };
        }

        public static List<Tuple<string, string>> GetPerformanceMapping()
        {
            return new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("FocusArea", "Focus Area"),
                    new Tuple<string, string>("Link", "Link"),
                    new Tuple<string, string>("Title", "Title"),
                    new Tuple<string, string>("Assignee", "Assignee"),
                    new Tuple<string, string>("Milestone", "Milestone"),
                    new Tuple<string, string>("Release", "Release"),
                    new Tuple<string, string>("Notes", "Notes"),
                };
        }

        public static List<Tuple<string, string>> GetEngineeringExcellenceMapping()
        {
            return new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("Link", "Link"),
                    new Tuple<string, string>("Title", "Title"),
                    new Tuple<string, string>("Assignee", "Assignee"),
                    new Tuple<string, string>("Milestone", "Milestone"),
                };
        }

        private static bool IsPackageSourceMappingIssue(Issue e)
        {
            return e.Labels.Any(e => e.Name.Equals(SourceMappingLabel));
        }

        private static string GetMapping(List<Tuple<string, string>> columnMapping, string from)
        {
            return columnMapping.FirstOrDefault(e => e.Item1.Equals(from))?.Item2 ?? from;
        }

        public static string ToMarkdownTable<T>(this IEnumerable<T> source, List<Tuple<string, string>> columnMapping)
        {
            var properties = typeof(T).GetRuntimeProperties().Where(x => columnMapping.Any(e => e.Item1.Equals(x.Name))).OrderBy(e => columnMapping.FindIndex(c => c.Item1.Equals(e.Name)));

            var fields = typeof(T)
                .GetRuntimeFields()
                .Where(f => f.IsPublic);

            var gettables = Enumerable.Union(
                properties.Select(p => new { p.Name, GetValue = (Func<object, object>)p.GetValue, Type = p.PropertyType }),
                fields.Select(p => new { p.Name, GetValue = (Func<object, object>)p.GetValue, Type = p.FieldType }));

            var maxColumnValues = source
                .Select(x => gettables.Select(p => !p.Name.Equals("Link") ? p.GetValue(x)?.ToString()?.Length ?? 0 : 0))
                .Union(new[] { gettables.Select(p => p.Name.Length) }) // Include header in column sizes
                .Aggregate(
                    new int[gettables.Count()].AsEnumerable(),
                    (accumulate, x) => accumulate.Zip(x, Math.Max))
                .ToArray();

            var columnNames = gettables.Select(p => p.Name);

            var headerLine = "| " + string.Join(" | ", columnNames.Select((n, i) => GetMapping(columnMapping, n).PadRight(maxColumnValues[i]))) + " |";

            var isNumeric = new Func<Type, bool>(type =>
                type == typeof(Byte) ||
                type == typeof(SByte) ||
                type == typeof(UInt16) ||
                type == typeof(UInt32) ||
                type == typeof(UInt64) ||
                type == typeof(Int16) ||
                type == typeof(Int32) ||
                type == typeof(Int64) ||
                type == typeof(Decimal) ||
                type == typeof(Double) ||
                type == typeof(Single));

            var rightAlign = new Func<Type, char>(type => isNumeric(type) ? ':' : ' ');

            var headerDataDividerLine =
                "| " +
                 string.Join(
                     "| ",
                     gettables.Select((g, i) => new string('-', maxColumnValues[i]) + rightAlign(g.Type))) +
                "|";

            var lines = new[]
                {
                    headerLine,
                    headerDataDividerLine,
                }.Union(
                    source
                    .Select(s =>
                        "| " + string.Join(" | ", gettables.Select((n, i) => (!n.Name.Equals("Link") ? n.GetValue(s)?.ToString() ?? "" : Linkify(n.GetValue(s).ToString())).PadRight(maxColumnValues[i]))) + " |"));

            return lines
                .Aggregate((p, c) => p + Environment.NewLine + c);
        }

        private static string Linkify(string v)
        {
            var value = v.Split('/').Last();
            return $"[{value}]({v})";
        }
    }

    public class IssueModel
    {
        public string Link { get; }
        public string Title { get; }
        public string Assignee { get; }
        public string Milestone { get; }
        public string Release { get; }
        public string FocusArea { get; }
        public string Notes { get; }

        public IssueModel(string link, string title, string assignee, string milestone, string release, string focusArea)
        {
            Link = link;
            Title = title;
            Assignee = assignee;
            Milestone = milestone;
            Release = release;
            FocusArea = focusArea;
        }

        public IssueModel(Issue e)
        {
            Link = e.HtmlUrl;
            Title = e.Title;
            Assignee = string.Join(",", e.Assignees.Select(e => e.Login));
            Milestone = e.Milestone?.Title;
            Release = string.Empty;
            FocusArea = string.Empty;
        }
    }

    public class Table
    {
        public IReadOnlyList<string> Headers { get; }
        public IList<IReadOnlyList<string>> Rows { get; }

        public Table(IReadOnlyList<string> headers)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public bool AddRow(IReadOnlyList<string> row)
        {
            if (Headers.Count == row.Count)
            {
                Rows.Add(row);
                return true;
            }
            return false;
        }
    }

}