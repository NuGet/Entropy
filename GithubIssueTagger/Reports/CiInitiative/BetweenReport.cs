using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace GithubIssueTagger.Reports.CiInitiative
{
    internal class BetweenReport
    {
        internal static Command GetCommand(GitHubPatBinder patBinder, Option<CiInitiativeReport.OutputFormat> format, Option<CiInitiativeReport.Order> order)
        {
            var command = new Command("between");
            command.Description = "Get completed CI Initiative between two dates";

            var start = new Option<DateOnly>("--start");
            start.AddAlias("-s");
            start.Description = "Start date";
            start.IsRequired = true;
            start.AddValidator(DateOnlyValidator);
            command.Add(start);
            var startBinder = new DateOnlyBinder(start);

            var end = new Option<DateOnly>("--end");
            end.AddAlias("-e");
            end.Description = " End date";
            end.IsRequired = true;
            end.AddValidator(DateOnlyValidator);
            command.Add(end);
            var endBinder = new DateOnlyBinder(end);

            command.Add(format);
            command.Add(order);

            command.SetHandler<GitHubPat, DateOnly, DateOnly, CiInitiativeReport.OutputFormat, CiInitiativeReport.Order>(RunAsync,
                patBinder, startBinder, endBinder, format, order);

            return command;

            static void DateOnlyValidator(OptionResult result)
            {
                string? str = result.GetValueOrDefault<string>();
                if (str == null)
                {
                    return;
                }
                try
                {
                    _ = DateOnly.Parse(str);
                }
                catch (FormatException exception)
                {
                    result.ErrorMessage = exception.Message;
                }
            }
        }

        private static async Task RunAsync(GitHubPat pat, DateOnly start, DateOnly end, CiInitiativeReport.OutputFormat format, CiInitiativeReport.Order order)
        {
            var serviceProvider = new ServiceCollection()
                .AddGithubIssueTagger(pat)
                .BuildServiceProvider();

            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using (scopeFactory.CreateScope())
            {
                var client = serviceProvider.GetRequiredService<GitHubClient>();

                var args = new CiInitiativeReport.Args
                {
                    Start = start,
                    End = end,
                    OutputFormat = format,
                    Order = order,
                };
                await CiInitiativeReport.RunAsync(client, args);
            }
        }

        private class DateOnlyBinder : BinderBase<DateOnly>
        {
            private readonly Option<DateOnly> _dateOnly;

            public DateOnlyBinder(Option<DateOnly> dateOption)
            {
                _dateOnly = dateOption;
            }

            protected override DateOnly GetBoundValue(BindingContext bindingContext)
            {
                string? result = bindingContext.ParseResult.FindResultFor(_dateOnly)?.GetValueOrDefault<string>();
                if (result == null)
                {
                    throw new Exception(_dateOnly.Name + " must be specified");
                }
                try
                {
                    DateOnly date = DateOnly.Parse(result);
                    return date;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
    }
}
