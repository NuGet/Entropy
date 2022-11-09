using Octokit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GithubIssueTagger.Reports.CiInitiative
{
    internal class MarkdownOutput
    {
        private static readonly IReadOnlyList<string> CompleteIcons = new[] { "🥳", "🤩", "👏", "🎈", "🎉", "🎊", "🪄", "🎂", "🧁", "🚀", "⛲", "🌝", "🌞", "⭐", "🌟", "🌈", "💯" };

        public static void Write(IEnumerable<Issue> issues)
        {
            var dt = ConvertToDataTable(issues);

            Console.OutputEncoding = Encoding.UTF8;
            WriteHeader(dt.Columns);
            foreach (DataRow row in dt.Rows)
            {
                WriteRow(row);
            }
        }

        // Use DataTable to eliminate risk of mismatched column counts in rows/header. Perf not a concern here.
        private static DataTable ConvertToDataTable(IEnumerable<Issue> issues)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("What");
            dt.Columns.Add("Who");
            dt.Columns.Add("Status");
            dt.Columns.Add("Impact/Progress");

            var icons = GetIcons();

            foreach (Issue issue in issues)
            {
                var row = dt.NewRow();
                row["What"] = $"[{issue.Title}]({issue.HtmlUrl})";
                row["Who"] = string.Join(", ", issue.Assignees.Select(a => a.Login));
                row["Status"] = string.Format("{0} Complete", icons[dt.Rows.Count % icons.Count]);
                dt.Rows.Add(row);
            }

            return dt;
        }

        private static void WriteHeader(DataColumnCollection columns)
        {
            Console.Write(columns[0].ColumnName);
            for (int i = 1; i < columns.Count; i++)
            {
                Console.Write('|');
                Console.Write(columns[i].ColumnName);
            }
            Console.WriteLine();

            Console.Write("---");
            for (int i = 1; i < columns.Count; i++)
            {
                Console.Write("|---");
            }
            Console.WriteLine();
        }

        private static void WriteRow(DataRow row)
        {
            Console.Write(row[0]);
            int columns = row.Table.Columns.Count;
            for (int i = 1; i < columns; i++)
            {
                Console.Write('|');
                Console.Write(row[i]);
            }

            Console.WriteLine();
        }

        // Make the reports a little less boring by using a different, random order each time.
        private static IReadOnlyList<string> GetIcons()
        {
            var icons = new string[CompleteIcons.Count];
            List<int> indicies = Enumerable.Range(0, icons.Length).ToList();

            var rand = new Random();
            for (int i = 0; i < icons.Length; i++)
            {
                Debug.Assert(indicies.Count > 0);
                var index = rand.Next(0, indicies.Count);
                icons[i] = CompleteIcons[indicies[index]];
                indicies.RemoveAt(index);
            }

            Debug.Assert(indicies.Count == 0);
            return icons;
        }
    }
}
