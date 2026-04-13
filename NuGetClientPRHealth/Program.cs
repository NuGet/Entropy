using NuGetDashboard;

int ParseArg(string name, int defaultVal)
{
    var raw = args.FirstOrDefault(a => a.StartsWith($"--{name}="))?.Split('=', 2).Last();
    return raw is not null && int.TryParse(raw, out var v) && v > 0 ? v : defaultVal;
}

var token      = args.FirstOrDefault(a => a.StartsWith("--token="))?.Split('=', 2).Last()
               ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
var windowDays = ParseArg("days", 14);
var outputPath = args.FirstOrDefault(a => a.StartsWith("--output="))?.Split('=', 2).Last()
               ?? $"nuget-pr-health-{DateTime.UtcNow:yyyy-MM-dd}.html";

Console.WriteLine();
Console.WriteLine("  NuGet.Client PR Health Dashboard");
Console.WriteLine("  ══════════════════════════════════");
Console.WriteLine($"  Window : past {windowDays} days");
Console.WriteLine($"  Output : {outputPath}");
Console.WriteLine();

if (token is null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Warning: No GitHub token found (rate limit: 60 req/hr).");
    Console.WriteLine("     Set GITHUB_TOKEN or pass --token=<pat>");
    Console.ResetColor();
    Console.WriteLine();
}

try
{
    using var client = new GitHubClient(token);
    var data = await new DashboardService(client, windowDays).BuildDashboardAsync();

    Console.Write("\n  Generating report... ");
    HtmlGenerator.Generate(data, outputPath);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Done!");
    Console.ResetColor();
    Console.WriteLine($"  Saved -> {Path.GetFullPath(outputPath)}");
    Console.WriteLine();
}
catch (Exception ex)
{
    var inner = ex is AggregateException ae ? ae.InnerExceptions.First() : ex;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n  Error: {inner.Message}");
    Console.ResetColor();
    Environment.Exit(1);
}
