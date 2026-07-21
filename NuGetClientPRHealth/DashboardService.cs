namespace NuGetDashboard;

public class DashboardService(GitHubClient client, int windowDays = 14)
{
    private static readonly HashSet<string> TeamMembers = new(StringComparer.OrdinalIgnoreCase)
    {
        "nkolev92", "zivkan", "jeffkl", "donnie-msft", "kartheekp-ms",
        "martinrrm", "jebriede", "Nigusu-Allehu", "aortiz-msft"
    };

    public async Task<DashboardData> BuildDashboardAsync()
    {
        var now       = DateTime.UtcNow;
        var windowAgo = now.AddDays(-windowDays);

        Console.Write($"  Fetching PRs merged in the past {windowDays} days... ");
        var rawPRs = (await client.SearchMergedPRsAsync(windowAgo, now))
            .Where(p => TeamMembers.Contains(p.Author))
            .ToList();
        Console.WriteLine($"{rawPRs.Count} found.");

        var callsNeeded = rawPRs.Count * 2; // timeline + reviews per PR
        var (coreRemaining, coreLimit) = await client.GetCoreRateLimitAsync();
        Console.WriteLine($"  Core API budget: {coreRemaining}/{coreLimit} remaining, {callsNeeded} needed.");
        if (coreRemaining < callsNeeded)
            throw new InvalidOperationException(
                $"GitHub core API rate limit too low: {coreRemaining} remaining, {callsNeeded} needed.\n" +
                $"  → Create a free classic token at github.com/settings/tokens/new?type=classic (no scopes needed)");

        Console.Write($"  Enriching {rawPRs.Count} PRs... ");
        var prs = await EnrichAsync(rawPRs);
        Console.WriteLine("done.");

        return new DashboardData(
            DateRange:  $"{windowAgo:MMM d} \u2013 {now:MMM d, yyyy}",
            AsOf:       now.ToString("MMMM d, yyyy") + " UTC",
            WindowDays: windowDays,
            Metrics:    ComputeMetrics(prs),
            SlowPRs:          prs.Where(p => p.HoursToMerge > 72).OrderByDescending(p => p.HoursToMerge).ToList(),
            SlowToReviewPRs:  prs.Where(p => p.FirstReviewHours > 24).OrderByDescending(p => p.FirstReviewHours).ToList(),
            AllPRs:           prs.OrderBy(p => p.MergedAt).ToList());
    }

    private async Task<List<PRRecord>> EnrichAsync(List<RawPR> prs)
    {
        var results = new List<PRRecord>();
        foreach (var raw in prs)
        {
            var readyAt        = await client.GetReadyTimeAsync(raw.Number);
            var effectiveStart = readyAt ?? raw.CreatedAt;
            var (reviewedAt, approvedAt) = await client.GetFirstReviewAndApprovalAsync(raw.Number, effectiveStart);

            results.Add(new PRRecord(
                raw.Number, raw.Title, raw.Url, raw.Author,
                raw.CreatedAt, effectiveStart, raw.MergedAt,
                HoursToMerge:       Math.Max(0, (raw.MergedAt - effectiveStart).TotalHours),
                FirstReviewHours:   reviewedAt.HasValue ? (reviewedAt.Value - effectiveStart).TotalHours : null,
                FirstReviewedAt:    reviewedAt,
                FirstApprovalHours: approvedAt.HasValue ? (approvedAt.Value - effectiveStart).TotalHours : null,
                FirstApprovedAt:    approvedAt));

            await Task.Delay(200); // avoid GitHub secondary rate limits
        }
        return results;
    }

    private static DashboardMetrics ComputeMetrics(List<PRRecord> prs)
    {
        if (prs.Count == 0) return new DashboardMetrics(0, 0, 0, 0);

        var sorted   = prs.Select(p => p.HoursToMerge).OrderBy(x => x).ToList();
        var n        = sorted.Count;
        var median   = n % 2 == 0 ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0 : sorted[n / 2];
        var reviewed = prs.Where(p => p.FirstApprovalHours.HasValue).ToList();

        return new DashboardMetrics(
            TotalPRs:                prs.Count,
            MedianHoursToComplete:   Math.Round(median, 1),
            PercentApprovedUnder24h: reviewed.Count > 0
                ? Math.Round((double)reviewed.Count(p => p.FirstApprovalHours! < 24) / reviewed.Count * 100, 1) : 0,
            PercentMergedUnder24h:   Math.Round((double)prs.Count(p => p.HoursToMerge < 24) / prs.Count * 100, 1));
    }
}
