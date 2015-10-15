using EnvDTE80;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace NuGetFunctionalTestsLauncher
{
    class Program
    {
        private static string NuGetPrefix = "NuGetFunctionalTests_";
        private static string CommandNameForPMC = "View.PackageManagerConsole";
        private static string TestPath { get; set; }
        private static string VSVersion { get; set; }
        private static string Command { get; set; }

        static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && (args[0] == "-debug" || args[0] == "--debug"))
                {
                    Debugger.Launch();
                    args = args.Skip(1).ToArray();
                }

                if (!GetInfo())
                {
                    return -1;
                }

                //KillExistingDevEnvInstances();

                Console.WriteLine("Launching VS...");

                // Launch VS
                var dte2 = GetDTE2();
                dte2.MainWindow.Activate();

                Console.WriteLine("Launched VS.");

                // Run functional tests
                RunFunctionalTests(dte2);

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAILED: " + typeof(Program).ToString() +
                    " failed to execute with the following error.");
                Console.Error.WriteLine(ex.ToString());
                return -1;
            }
        }

        private static bool GetInfo()
        {
            // Get Root directory
            TestPath = GetValue(NuGetPrefix + nameof(TestPath), Directory.GetCurrentDirectory());

            // Get VS Version
            VSVersion = GetValue(NuGetPrefix + nameof(VSVersion), "14.0");

            // Get Command to execute
            Command = GetValue(NuGetPrefix + nameof(Command), "Run-Test FindPackageByIdjQuery");

            // Test that nuget.tests.psm1, API.Test.dll, NuGet-Signed.exe,
            // and GenerateTestPackages.exe (+related files) are present
            if (MandatoryFileExistsAtTestPath(@"NuGet.Tests.psm1")
                && MandatoryFileExistsAtTestPath(@"API.Test.dll")
                && MandatoryFileExistsAtTestPath(@"NuGet-Signed.exe")
                && MandatoryFileExistsAtTestPath(@"GenerateTestPackages.exe")
                && MandatoryFileExistsAtTestPath(@"GenerateTestPackages.exe.config")
                && MandatoryFileExistsAtTestPath(@"NuGet.Core.dll"))
            {
                return true;
            }

            return false;
        }

        private static string GetValue(string key, string defaultValue)
        {
            var result = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(result))
            {
                // Default to 'defaultValue'
                result = defaultValue;
            }

            return result;
        }

        private static bool MandatoryFileExistsAtTestPath(string fileName)
        {
            var filePath = Path.Combine(TestPath, fileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine(filePath + " is not found");
                Console.Error.WriteLine(filePath + " is not found");
                return false;
            }

            return true;
        }

        private static void KillExistingDevEnvInstances()
        {
            Console.WriteLine("Getting running devenv processes...");
            var processes = Process.GetProcessesByName("devenv");

            foreach (var process in processes)
            {
                process.Kill();
            }

            if (processes.Any())
            {
                Console.WriteLine("Found '{0}' instance(s) of devenv and killed them", processes.Length);
            }
            else
            {
                Console.WriteLine("Found no existing instances of devenv");
            }
        }

        private static DTE2 GetDTE2()
        {
            // Get the ProgID for DTE 14.0.
            Type t = Type.GetTypeFromProgID(
                "VisualStudio.DTE." + VSVersion, true);
            // Create a new instance of the IDE.
            object obj = Activator.CreateInstance(t, true);
            // Cast the instance to DTE2 and assign to variable dte.
            DTE2 dte2 = (DTE2)obj;

            return dte2;
        }

        private static void RunFunctionalTests(DTE2 dte2)
        {
            Console.WriteLine("Wait for 10 seconds before executing the command so that VS can be activated");
            Thread.Sleep(TimeSpan.FromSeconds(10));

            Console.WriteLine("For testing, open output window");
            dte2.ExecuteCommand("View.Output");

            Console.WriteLine("Executing View.PackageManagerConsole menu on VS via DTE...");
            // Launch Powershell Console by executing View.PackageManagerConsole menu
            dte2.ExecuteCommand(CommandNameForPMC);

            Console.WriteLine("Wait for 5 secs for the powershell host to initialize before executing the next command");
            Thread.Sleep(TimeSpan.FromSeconds(5));

            Console.WriteLine("Import the NuGet.Tests powershell module so that 'Run-Test' can be executed");
            dte2.ExecuteCommand(CommandNameForPMC, @"Import-Module " + Path.Combine(TestPath, "nuget.Tests.psm1"));
            Thread.Sleep(TimeSpan.FromSeconds(2));

            Console.WriteLine("Import the API.Test.dll module so that 'Run-Test' can be executed");
            dte2.ExecuteCommand(CommandNameForPMC, @"Import-Module " + Path.Combine(TestPath, "API.Test.dll"));
            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Execute the main command now that the necessary modules have been imported
            dte2.ExecuteCommand(CommandNameForPMC, Command);

            Console.WriteLine("Started running functional tests...");
        }
    }
}
