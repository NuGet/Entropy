using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NuGetValidator
{
    public class Program
    {
        private static readonly string DebugOption = "--debug";
        private static readonly string AppName = "nugetvalidator";
        private static readonly string HelpOption = "-h|--help";


        public static int Main(string[] args)
        {
#if DEBUG
            if (args.Contains(DebugOption))
            {
                args = args.Where(arg => !StringComparer.OrdinalIgnoreCase.Equals(arg, DebugOption)).ToArray();

                Debugger.Launch();
            }
#endif

            var app = InitializeApp();

            // Register commands
            RegisterCommands(app);

            app.OnExecute(() =>
            {
                app.ShowHelp();

                return 0;
            });

            Console.WriteLine(string.Format("{0} Version: {1}", app.FullName, app.LongVersionGetter()));

            var exitCode = 0;

            try
            {
                exitCode = app.Execute(args);
            }
            catch (Exception e)
            {
                // Log the error
                Console.WriteLine(e);
                exitCode = 1;
            }

            // Limit the exit code range to 0-255 to support POSIX
            if (exitCode < 0 || exitCode > 255)
            {
                exitCode = 1;
            }

            return exitCode;

        }


        private static CommandLineApplication InitializeApp()
        {
            var app = new CommandLineApplication()
            {
                Name = AppName,
                FullName = AppName
            };

            app.HelpOption(HelpOption);
            app.VersionOption("--version", typeof(Program).GetTypeInfo().Assembly.GetName().Version.ToString());

            return app;
        }

        private static void RegisterCommands(CommandLineApplication app)
        {
            // Register commands
            LocalizationValidatorCommand.Register(app);
        }
    }
}