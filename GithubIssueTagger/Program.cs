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
patOption.Description = "GitHub Personal Access Token to make API calls with.";
patOption.AddAlias("-p");

var patBinder = new GitHubPatBinder(patOption);

var interactiveCommand = new Command(
"--interactive",
"Run in interactive mode.");
interactiveCommand.AddAlias("-i");
interactiveCommand.SetHandler(
    async (GitHubPat pat) => await RunInteractiveModeAsync(pat, allReportTypes),
    patBinder);

var rootCommand = new RootCommand
{
    patOption,
    interactiveCommand
};

patOption.Description = "GitHub Personal Access Token. If none is supplied, the environment variable GITHUB_TOKEN is checked, and if empty, an attempt to get one from the git credential provider will be made.";
rootCommand.Description = "NuGet.Client tool to generate reports from GitHub issues.";

var simpleCommandFactory = new SimpleCommandFactory();
foreach (var reportType in allReportTypes)
{
    ICommandFactory commandFactory = GetCommandFactory(reportType) ?? simpleCommandFactory;
    var reportCommand = commandFactory.CreateCommand(reportType, patBinder);
    rootCommand.AddCommand(reportCommand);
}

var exitCode = await rootCommand.InvokeAsync(args);
return exitCode;

ICommandFactory? GetCommandFactory(Type reportType)
{
    var commandFactoryAttribute = reportType.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(CommandFactoryAttribute));
    if (commandFactoryAttribute == null)
    {
        return null;
    }

    var attributeInstance = (CommandFactoryAttribute)commandFactoryAttribute.Constructor.Invoke(commandFactoryAttribute.ConstructorArguments.Select(arg => arg.Value).ToArray());
    var commandFactory = (ICommandFactory?)Activator.CreateInstance(attributeInstance.FactoryType);
    if (commandFactory == null)
    {
        throw new Exception("Unable to create command factory " + attributeInstance.FactoryType.FullName);
    }
    return commandFactory;
}

static async Task RunInteractiveModeAsync(GitHubPat pat, IReadOnlyList<Type> reportTypes)
{
    var serviceProvider = new ServiceCollection()
        .AddGithubIssueTagger(pat)
        .BuildServiceProvider();
    var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

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
            using (scopeFactory.CreateScope())
            {
                var report = (IReport)serviceProvider.GetRequiredService(reportToRun);
                await report.RunAsync();
            }
            Console.WriteLine("*** Done Executing " + reportToRun.Name + " ***");
        }
    }
}
