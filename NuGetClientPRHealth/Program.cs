using NuGetDashboard;

int ParseArg(string name, int defaultVal)
{
    var raw = args.FirstOrDefault(a => a.StartsWith($"--{name}="))?.Split('=', 2).Last();
    return raw is not null && int.TryParse(raw, out var v) && v > 0 ? v : defaultVal;
}

static bool IsUnauthorized(Exception ex) =>
    (ex is AggregateException ae ? ae.InnerExceptions.First() : ex)
        .Message.Contains("401") || 
    (ex is AggregateException ae2 ? ae2.InnerExceptions.First() : ex)
        .Message.Contains("authentication failed", StringComparison.OrdinalIgnoreCase);

var gitUri          = new Uri("https://github.com/nuget/home");
var explicitToken   = args.FirstOrDefault(a => a.StartsWith("--token="))?.Split('=', 2).Last()
                   ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
var windowDays      = ParseArg("days", 14);
var outputPath      = args.FirstOrDefault(a => a.StartsWith("--output="))?.Split('=', 2).Last()
                   ?? $"nuget-pr-health-{DateTime.UtcNow:yyyy-MM-dd}.html";

Console.WriteLine();
Console.WriteLine("  NuGet.Client PR Health Dashboard");
Console.WriteLine("  ══════════════════════════════════");
Console.WriteLine($"  Window : past {windowDays} days");
Console.WriteLine($"  Output : {outputPath}");
Console.WriteLine();

// Resolve token: explicit arg/env takes priority, otherwise ask git credential manager.
GitToken? gitToken = null;
string? token;
if (explicitToken is not null)
{
    token = explicitToken;
}
else
{
    gitToken = GitCredentials.Get(gitUri);
    token = gitToken?.Password;
}

if (token is null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Warning: No GitHub token found (rate limit: 60 req/hr).");
    Console.WriteLine("     Set GITHUB_TOKEN or pass --token=<pat>");
    Console.ResetColor();
    Console.WriteLine();
}

async Task<bool> TryRunAsync(string? tok)
{
    try
    {
        using var client = new GitHubClient(tok);
        var data = await new DashboardService(client, windowDays).BuildDashboardAsync();

        Console.Write("\n  Generating report... ");
        HtmlGenerator.Generate(data, outputPath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Done!");
        Console.ResetColor();
        Console.WriteLine($"  Saved -> {Path.GetFullPath(outputPath)}");
        Console.WriteLine();
        return true;
    }
    catch (Exception ex) when (IsUnauthorized(ex))
    {
        return false;
    }
}

if (!await TryRunAsync(token))
{
    // Token was rejected (401). If it came from git credentials, erase it so the
    // credential manager will prompt for a fresh one, then retry once.
    if (gitToken is not null)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Saved credential was rejected (401). Requesting fresh login...");
        Console.ResetColor();
        GitCredentials.Reject(gitToken);
    }

    gitToken = GitCredentials.Get(gitUri);
    token = gitToken?.Password;

    if (!await TryRunAsync(token))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n  Error: GitHub authentication failed. Check your token and try again.");
        Console.ResetColor();
        Environment.Exit(1);
    }
}

