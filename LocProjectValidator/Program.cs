using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace LocProjectValidator
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Argument<string>(
                    "loc-project-file",
                    description: "Input path to LocProject.json file"),
                new Argument<string>(
                    "artifacts-dir",
                    description: "Localization Artifacts folder to validate LocProject.json file entries"),
                new Argument<string>(
                    "loc-repo-dir",
                    description: "Localization repository folder to validate LocProject.json file entries"),
            };

            rootCommand.Description = "Verifies LocProject.json file entries againts files contained in a AzDO localization artifact";

            rootCommand.Handler = CommandHandler.Create<string, string, string>(Run);

            return await rootCommand.InvokeAsync(args);
        }

        private static void Run(string locProjectFile, string artifactsDir, string locRepoDir)
        {
            var text = File.ReadAllText(locProjectFile);
            var locProject = JsonSerializer.Deserialize<LocProject>(text);

            foreach (var prj in locProject.Projects)
            {
                foreach (var locItem in prj.LocItems)
                {
                    ValidateFile(locItem.SourceFile, nameof(locItem.SourceFile), artifactsDir, locItem);
                    ValidateFile(locItem.LcgFile, nameof(locItem.LcgFile), artifactsDir, locItem);
                    ValidateFile(locItem.LclFile, nameof(locItem.LclFile), locRepoDir, locItem);
                    ValidateFile(locItem.LciFile, nameof(locItem.LciFile), locRepoDir, locItem);
                }
            }
        }

        private static void ValidateFile(string locPath, string attributeName, string workingDir, LocItem rootObj)
        {
            if (string.IsNullOrEmpty(locPath))
            {
                Console.WriteLine("Warning: {0} is empty, source={1}", attributeName, rootObj.SourceFile);
                return;
            }
            var fullPath = Path.IsPathRooted(locPath) ? locPath : Path.Combine(workingDir, locPath);

            if (fullPath.Contains("{Lang}"))
            {
                foreach(var pi in typeof(LanguageFolderMappings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    fullPath = fullPath.Replace("{Lang}", pi.Name);

                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine("Warning: {0}={1} does not exists", attributeName, fullPath);
                    }
                }
            }
            else
            {
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine("Warning: {0}={1} does not exists", attributeName, fullPath);
                }
            }
        }
    }
}
