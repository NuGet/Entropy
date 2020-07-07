using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public static class PlanningUtilities
    {
        public static async Task RunPlanningAsync(GitHubClient client)
        {
            IEnumerable<Issue> issues = await GetPerformanceIssuesForSprint(client);

            var markdownTable = issues.Select(e => new IssueModel(e)).ToMarkdownTable(GetPerformanceMapping());

            Console.WriteLine();
            Console.WriteLine(markdownTable);
            Console.WriteLine();
            Console.ReadKey();
        }

        public static async Task<IEnumerable<Issue>> GetPerformanceIssuesForSprint(GitHubClient client)
        {
            Predicate<Issue> isRelevant = (Issue x) => IsPerformance(x);
            // 111 is Sprint 173
            var homeIssues = await IssueUtilities.GetIssuesForMilestone(client, "nuget", "home", "111", isRelevant);
            // 15 is Sprint 173
            var clientEngineeringIssues = await IssueUtilities.GetIssuesForMilestone(client, "nuget", "client.engineering", "15", isRelevant);
            var issues = homeIssues.Union(clientEngineeringIssues);

            return issues;

            static bool IsPerformance(Issue e)
            {
                var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                aliases.Add("nkolev92");
                aliases.Add("srdjanjovcic");
                aliases.Add("donnie-msft");
                aliases.Add("dominofire");
                aliases.Add("erdembayar");
                return aliases.Contains(e.Assignee?.Login) && e.Labels.Any(e => e.Name.Equals("Tenet:Performance"));
            }
        }

        public static async Task<IEnumerable<Issue>> GetEngineeringExcellenceIssuesForSprint(GitHubClient client)
        {
            Predicate<Issue> isRelevant = (Issue x) => IsEngineeringExcellence(x);
            // 13 is Sprint 171
            var issues = await IssueUtilities.GetIssuesForMilestone(client, "nuget", "client.engineering", "13", isRelevant);

            return issues;

            static bool IsEngineeringExcellence(Issue e)
            {
                return e.Labels.Any(e => e.Name.Equals("Engineering Excellence"));
            }

        }

        public static async Task<IEnumerable<Issue>> GetPerformanceBacklog(GitHubClient client)
        {
            var homeIssues = await IssueUtilities.GetIssuesForLabel(client, "nuget", "home", "Tenet:Performance");
            var clientEngineeringIssues = await IssueUtilities.GetIssuesForLabel(client, "nuget", "client.engineering", "Tenet:Performance");
            var issues = homeIssues.Union(clientEngineeringIssues);
            return issues;
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

        private static string GetMapping(List<Tuple<string, string>> columnMapping, string from)
        {
            return columnMapping.FirstOrDefault(e => e.Item1.Equals(from))?.Item2 ?? from;
        }

        public static string ToMarkdownTable<T>(this IEnumerable<T> source, List<Tuple<string, string>> columnMapping)
        {
            var properties = typeof(T).GetRuntimeProperties().OrderBy(e => columnMapping.FindIndex(c => c.Item1.Equals(e.Name)));

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
            Assignee = e.Assignee?.Login;
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