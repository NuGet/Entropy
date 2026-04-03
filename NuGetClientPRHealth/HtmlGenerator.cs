namespace NuGetDashboard;

public static class HtmlGenerator
{
    public static void Generate(DashboardData data, string outputPath)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8">
            <style>
              body { font-family: Calibri, Arial, sans-serif; font-size: 11pt; color: #1F1F1F; max-width: 900px; }
              h1   { font-size: 14pt; color: #1F3864; }
              h2   { font-size: 12pt; color: #2E74B5; margin-top: 24px; }
              table { border-collapse: collapse; width: 100%; margin-bottom: 12px; }
              th { background: #1F3864; color: #fff; padding: 6px 10px; text-align: left; }
              td { padding: 5px 10px; border-bottom: 1px solid #D9D9D9; }
              tr:nth-child(even) td { background: #F2F2F2; }
              a  { color: #0563C1; }
              .key p { margin: 2px 0; }
            </style>
            </head><body>
            """);

        sb.AppendLine($"<h1>PR health over the past {data.WindowDays} days</h1>");

        // Metrics
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Measurement</th><th>Value</th></tr>");
        sb.AppendLine($"<tr><td>Total number of PRs in range</td><td>{data.Metrics.TotalPRs}</td></tr>");
        sb.AppendLine($"<tr><td>Median: Hours to complete</td><td>{data.Metrics.MedianHoursToComplete:F1}</td></tr>");
        sb.AppendLine($"<tr><td>Percentage of PRs approved under 24 hrs</td><td>{data.Metrics.PercentApprovedUnder24h:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Percentage of PRs completed under 24 hrs</td><td>{data.Metrics.PercentMergedUnder24h:F1}%</td></tr>");
        sb.AppendLine("</table>");

        // Slow PRs
        sb.AppendLine($"<h2>Long lived PRs (closed after 72 hrs): the past {data.WindowDays} days</h2>");
        if (data.SlowPRs.Count == 0)
        {
            sb.AppendLine("<p>🎉 All PRs closed within 72 hours this period!</p>");
        }
        else
        {
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>PR link</th><th>Hours to close</th><th>Why so long?</th></tr>");
            foreach (var pr in data.SlowPRs)
                sb.AppendLine($"<tr><td><a href=\"{pr.Url}\">{pr.Url}</a></td><td>{pr.HoursToMerge:F2}</td><td></td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<div class='key'>");
            sb.AppendLine("<strong>Why so long Key</strong>");
            foreach (var line in new[]
            {
                "💔 - Delayed getting reviews from PR Buddy™️ or Team",
                "🤖 - Testing infrastructure, delayed intentionally",
                "🌪️ - PR fell behind other priorities",
                "⛱️ - Weekend/PTO delayed",
                "🛠️ - Merging Blocked (ex. CI Failures, rule violations)",
                "📜 - Lots of feedback",
                "🙂 - No issues - just letting more people chime in",
            })
                sb.AppendLine($"<p>{line}</p>");
            sb.AppendLine("</div>");
        }

        // Appendix
        sb.AppendLine($"<h2>Appendix — All PRs in Period ({data.AllPRs.Count})</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>PR</th><th>Title</th><th>Created (UTC)</th><th>Ready for Review</th><th>First Approved</th><th>Merged (UTC)</th><th>Duration</th></tr>");
        foreach (var pr in data.AllPRs)
        {
            var approved = pr.FirstApprovedAt.HasValue ? Ts(pr.FirstApprovedAt.Value) : "—";
            sb.AppendLine($"<tr><td><a href=\"{pr.Url}\">#{pr.Number}</a></td><td>{H(pr.Title)}</td><td>{Ts(pr.CreatedAt)}</td><td>{Ts(pr.EffectiveStart)}</td><td>{approved}</td><td>{Ts(pr.MergedAt)}</td><td>{FormatHours(pr.HoursToMerge)}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("</body></html>");

        File.WriteAllText(outputPath, sb.ToString(), System.Text.Encoding.UTF8);
    }

    private static string Ts(DateTime dt) => dt.ToUniversalTime().ToString("MMM d HH:mm");
    private static string H(string s) => System.Net.WebUtility.HtmlEncode(s);
    private static string FormatHours(double h) =>
        h < 24 ? $"{h:F1}h" : $"{(int)(h / 24)}d {(int)(h % 24)}h";
}
