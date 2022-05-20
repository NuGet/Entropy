using GithubIssueTagger;
using GithubIssueTagger.Reports;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

var allReportTypes = typeof(Program).Assembly.GetTypes().Where(t => t.IsClass && t.IsAssignableTo(typeof(IReport))).OrderBy(t => t.Name).ToList();

var patOption = new Option<string>("--pat");
patOption.AddAlias("-p");

var interactiveCommand = new Command(
"--interactive",
"Run in interactive mode.");
interactiveCommand.AddAlias("-i");
interactiveCommand.SetHandler(
    async (string pat) => await RunInteractiveMode(null, allReportTypes),
    patOption);

var rootCommand = new RootCommand
{
    patOption,
    interactiveCommand
};

patOption.Description = "GitHub Personal Access Token. If none is supplied, an attempt to get one from the git credential provider will be made.";
rootCommand.Description = "NuGet.Client tool to generate reports from GitHub issues.";

foreach (var reportType in allReportTypes)
{
    var reportCommand = new Command(reportType.Name);
    reportCommand.SetHandler(async (string pat) =>
    {
        var githubClient = GetGitHubClient(pat);
        var serviceProvider = GetSericeProvider(githubClient, allReportTypes);
        IReport report = (IReport)serviceProvider.GetRequiredService(reportType);
        await report.Run();
    }, patOption);

    rootCommand.AddCommand(reportCommand);
}

var exitCode = await rootCommand.InvokeAsync(args);
return exitCode;

static GitHubClient GetGitHubClient(string pat)
{
    var client = new GitHubClient(new ProductHeaderValue("nuget-github-issue-tagger"));

    if (!string.IsNullOrEmpty(pat))
    {
        client.Credentials = new Credentials(pat);
    }
    else
    {
        Dictionary<string, string> credentuals = GitCredentials.Get(new Uri("https://github.com/NuGet/Home"));
        if (credentuals?.TryGetValue("password", out string password) == true)
        {
            client.Credentials = new Credentials(password);
        }
        else
        {
            Console.WriteLine("Warning: Unable to get github token. Making unauthenticated HTTP requests, which has lower request limits.");
        }
    }

    return client;
}

static IServiceProvider GetSericeProvider(GitHubClient githubClient, IEnumerable<Type> reports)
{
    var services = new ServiceCollection();

    services.AddSingleton(githubClient);
    services.AddSingleton<QueryCache>();

    foreach (var report in reports)
    {
        services.AddSingleton(report);
    }

    IServiceProvider serviceProvider = services.BuildServiceProvider();
    return serviceProvider;
}

static async Task RunInteractiveMode(string pat, IReadOnlyList<Type> reportTypes)
{
    var client = GetGitHubClient(pat);
    var serviceProvider = GetSericeProvider(client, reportTypes);

    Console.WriteLine("**********************************************************************");
    Console.WriteLine("******************* NuGet GitHub Issue Tagger ************************");
    Console.WriteLine("**********************************************************************");
    Console.WriteLine();

    for (; ; )
    {
        for (int i = 0; i < reportTypes.Count; i++)
        {
            Console.WriteLine("{0}: {1}", i, reportTypes[i].Name);
        }
        Console.WriteLine("Enter a # to query or 'quit' to exit: ");

        var input = Console.ReadLine();

        Type? reportToRun;
        if (StringComparer.CurrentCultureIgnoreCase.Equals("quit", input))
        {
            return;
        }
        if (int.TryParse(input, out int index) && index >= 0 && index < reportTypes.Count)
        {
            reportToRun = reportTypes[index];
        }
        else
        {
            reportToRun = null;
            for (int i = 0; i < reportTypes.Count; i++)
            {
                if (StringComparer.CurrentCultureIgnoreCase.Equals(i, reportTypes[i].Name))
                {
                    reportToRun = reportTypes[i];
                    break;
                }
            }
        }

        if (reportToRun == null)
        {
            Console.WriteLine("Unknown query '" + input + "'");
        }
        else
        {
            Console.WriteLine(reportToRun.Name + "***");
            var report = (IReport)serviceProvider.GetRequiredService(reportToRun);
            await report.Run();
            Console.WriteLine("*** Done Executing " + reportToRun.Name + " ***");
        }
    }
}
