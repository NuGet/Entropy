using System.Net.Http.Headers;
using System.Text.Json;

namespace NuGetDashboard;

public sealed class GitHubClient : IDisposable
{
    private readonly HttpClient _http;
    private const string Repo = "NuGet/NuGet.Client";
    private int _rateLimitRemaining = int.MaxValue;

    public int RateLimitRemaining => _rateLimitRemaining;

    /// <summary>
    /// Calls /rate_limit (free — not counted against quota) and returns
    /// the core API remaining budget, which is what timeline/review calls consume.
    /// </summary>
    public async Task<(int remaining, int limit)> GetCoreRateLimitAsync()
    {
        using var resp = await _http.GetAsync("rate_limit");
        if (!resp.IsSuccessStatusCode) return (int.MaxValue, int.MaxValue);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var core = doc.RootElement.GetProperty("resources").GetProperty("core");
        return (core.GetProperty("remaining").GetInt32(), core.GetProperty("limit").GetInt32());
    }

    public GitHubClient(string? token = null)
    {
        _http = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NuGetDashboardCli/1.0");
        // Include mockingbird preview to ensure timeline events are returned
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.mockingbird-preview+json");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        if (token is not null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<RawPR>> SearchMergedPRsAsync(DateTime since, DateTime until)
    {
        var results = new List<RawPR>();
        var q = Uri.EscapeDataString($"repo:{Repo} is:pr is:merged merged:{since:yyyy-MM-dd}..{until:yyyy-MM-dd}");

        for (var page = 1; ; page++)
        {
            using var resp = await _http.GetAsync($"search/issues?q={q}&per_page=100&page={page}");
            TrackRateLimit(resp);
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var items = doc.RootElement.GetProperty("items");
            foreach (var item in items.EnumerateArray())
                results.Add(ParseRawPR(item));
            if (items.GetArrayLength() < 100) break;
        }
        return results;
    }

    /// <summary>
    /// Returns the time the PR became ready for review:
    /// first ready_for_review event → first review_requested event → null (caller falls back to created_at).
    /// </summary>
    public async Task<DateTime?> GetReadyTimeAsync(int prNumber)
    {
        using var resp = await _http.GetAsync(
            $"repos/{Repo}/issues/{prNumber}/timeline?per_page=100");
        TrackRateLimit(resp);
        await ThrowIfErrorAsync(resp, $"timeline for PR #{prNumber}");
        if (!resp.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

        DateTime? readyForReview = null;
        DateTime? firstReviewRequest = null;

        foreach (var ev in doc.RootElement.EnumerateArray())
        {
            if (!ev.TryGetProperty("event", out var evProp)) continue;
            if (!ev.TryGetProperty("created_at", out var tsProp)) continue;
            if (!tsProp.TryGetDateTime(out var ts)) continue;

            switch (evProp.GetString())
            {
                case "ready_for_review":
                    if (readyForReview is null || ts < readyForReview)
                        readyForReview = ts;
                    break;
                case "review_requested":
                    if (firstReviewRequest is null || ts < firstReviewRequest)
                        firstReviewRequest = ts;
                    break;
            }
        }
        return readyForReview ?? firstReviewRequest;
    }

    /// <summary>Returns the DateTime of the first APPROVED review, or null.</summary>
    public async Task<DateTime?> GetFirstApprovalAtAsync(int prNumber)
    {
        using var resp = await _http.GetAsync($"repos/{Repo}/pulls/{prNumber}/reviews");
        TrackRateLimit(resp);
        await ThrowIfErrorAsync(resp, $"reviews for PR #{prNumber}");
        if (!resp.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        DateTime? firstApproval = null;

        foreach (var r in doc.RootElement.EnumerateArray())
        {
            if (r.GetProperty("state").GetString() != "APPROVED") continue;
            if (!r.TryGetProperty("submitted_at", out var el)) continue;
            if (!el.TryGetDateTime(out var t)) continue;
            if (firstApproval is null || t < firstApproval) firstApproval = t;
        }
        return firstApproval;
    }

    private void TrackRateLimit(HttpResponseMessage resp)
    {
        if (resp.Headers.TryGetValues("X-RateLimit-Remaining", out var vals) &&
            int.TryParse(vals.FirstOrDefault(), out var remaining))
            _rateLimitRemaining = remaining;
    }

    private static async Task ThrowIfErrorAsync(HttpResponseMessage resp, string context)
    {
        if (resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync();

        // Distinguish the three common failure modes from the response body
        if (body.Contains("secondary rate limit", StringComparison.OrdinalIgnoreCase) ||
            resp.Headers.Contains("Retry-After"))
        {
            throw new InvalidOperationException(
                $"GitHub secondary rate limit (abuse detection) hit fetching {context}.\n" +
                $"  → Wait a minute then retry.");
        }

        if (resp.Headers.TryGetValues("X-RateLimit-Remaining", out var v) && v.FirstOrDefault() == "0")
        {
            throw new InvalidOperationException(
                $"GitHub primary rate limit exhausted fetching {context}.\n" +
                $"  → Wait until the hour resets, or use a different token.");
        }

        if (body.Contains("Resource not accessible by integration", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("must have push access", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Permission denied fetching {context}.\n" +
                $"  Your token is a fine-grained PAT — it needs these permissions:\n" +
                $"    • Issues: Read\n" +
                $"    • Pull requests: Read\n" +
                $"  → Edit the token at github.com/settings/tokens and add those, then retry.\n" +
                $"  → Or use a classic token (github.com/settings/tokens/new?type=classic) with no scopes.\n" +
                $"  Raw error: {body}");
        }

        if (body.Contains("forbids access via a fine-grained personal access token", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The NuGet org blocks fine-grained PATs with lifetime > 7 days.\n" +
                $"  → Use a classic token instead (recommended — no scopes needed):\n" +
                $"       github.com/settings/tokens/new?type=classic\n" +
                $"  → Or shorten your fine-grained PAT's lifetime to ≤7 days at:\n" +
                $"       github.com/settings/personal-access-tokens");
        }

        throw new InvalidOperationException(
            $"GitHub API {(int)resp.StatusCode} error fetching {context}.\n  Body: {body}");
    }

    private static RawPR ParseRawPR(JsonElement item)
    {
        var pr = item.GetProperty("pull_request");
        DateTime mergedAt;
        if (pr.TryGetProperty("merged_at", out var mergedAtProp) &&
            mergedAtProp.ValueKind != JsonValueKind.Null &&
            mergedAtProp.TryGetDateTime(out var mergedAtValue))
        {
            mergedAt = mergedAtValue;
        }
        else
        {
            mergedAt = item.GetProperty("closed_at").GetDateTime();
        }

        return new RawPR(
            Number:    item.GetProperty("number").GetInt32(),
            Title:     item.GetProperty("title").GetString()!,
            Url:       item.GetProperty("html_url").GetString()!,
            Author:    item.GetProperty("user").GetProperty("login").GetString()!,
            CreatedAt: item.GetProperty("created_at").GetDateTime(),
            MergedAt:  mergedAt);
    }

    public void Dispose() => _http.Dispose();
}
