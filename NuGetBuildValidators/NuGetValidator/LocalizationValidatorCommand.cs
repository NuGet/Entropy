using Microsoft.Extensions.CommandLineUtils;
using NuGetValidator.Localization;
using NuGetValidator.Utility;
using System;
using System.Linq;

namespace NuGetValidator
{
    internal static class LocalizationValidatorCommand
    {
        private static readonly string Description = "Validate the localization.";
        private static readonly string HelpOption = "-h|--help";
        private static readonly string VsixSwitchDescription = "Switch to indicate that a vsix needs to be validated. " +
                                                               "If -x|--vsix switch is provided, then the tool validates the NuGet vsix. " +
                                                               "Else the tool validates an artifacts location for the NuGet code base.";
        private static readonly string VsixPathDescription = "Path to NuGet.Tools.Vsix containing all english and translated dlls.";
        private static readonly string VsixExtractPathDescription = "Path to extract NuGet.Tools.Vsix into. Folder need not be present, but Program should have write access to the location.";
        private static readonly string OutputPathDescription = "Path to the directory for writing errors. File need not be present, but Program should have write access to the location.";
        private static readonly string CommentsPathDescription = "Path to the local NuGet localization repository. e.g. - <repo_root>\\Main\\localize\\comments\\15";
        private static readonly string ArtifactsPathDescription = "Path to the local NuGet artifacts folder. This option is used to validate a locally built NuGet repository.";


        public static void Register(CommandLineApplication app)
        {
            app.Command("localization", localizationValidator =>
            {
                localizationValidator.Description = Description;
                localizationValidator.HelpOption(HelpOption);

                var vsixSwitch = localizationValidator.Option(
                    "-x|--vsix",
                    VsixSwitchDescription,
                    CommandOptionType.NoValue);

                var vsixPath = localizationValidator.Option(
                    "-p|--vsix-path",
                    VsixPathDescription,
                    CommandOptionType.SingleValue);

                var vsixExtractPath = localizationValidator.Option(
                    "-e|--vsix-extract-path",
                    VsixExtractPathDescription,
                    CommandOptionType.SingleValue);

                var outputPath = localizationValidator.Option(
                    "-o|--output-path",
                    OutputPathDescription,
                    CommandOptionType.SingleValue);

                var commentsPath = localizationValidator.Option(
                    "-c|--comments-path",
                    CommentsPathDescription,
                    CommandOptionType.SingleValue);

                var artifactsPath = localizationValidator.Option(
                    "-a|--artifacts-path",
                    ArtifactsPathDescription,
                    CommandOptionType.SingleValue);

                localizationValidator.OnExecute(() =>
                {
                    var exitCode = 0;

                    if (vsixSwitch.HasValue())                   
                    {
                        if(!vsixPath.HasValue() || !vsixExtractPath.HasValue() || !outputPath.HasValue())
                        {
                            Console.WriteLine("Since -x|--vsix switch was passed, please enter the following arguments - ");
                            Console.WriteLine($"{vsixPath.ShortName}|{vsixPath.LongName}: {vsixPath.Description}");
                            Console.WriteLine($"{vsixExtractPath.ShortName}|{vsixExtractPath.LongName}: {vsixExtractPath.Description}");
                            Console.WriteLine($"{outputPath.ShortName}|{outputPath.LongName}: {outputPath.Description}");
                            exitCode = 1;
                        }
                        else
                        {
                            exitCode = LocalizationValidator.ExecuteForVsix(vsixPath.Value(), vsixExtractPath.Value(), outputPath.Value(), commentsPath.Value());
                        }
                    }
                    else
                    {
                        if (!artifactsPath.HasValue() || !outputPath.HasValue())
                        {
                            Console.WriteLine("Since -x|--vsix switch was not passed, please enter the following arguments - ");
                            Console.WriteLine($"{artifactsPath.ShortName}|{artifactsPath.LongName}: {artifactsPath.Description}");
                            Console.WriteLine($"{outputPath.ShortName}|{outputPath.LongName}: {outputPath.Description}");
                            exitCode = 1;
                        }
                        else
                        {
                            exitCode = LocalizationValidator.ExecuteForArtifacts(artifactsPath.Value(), outputPath.Value(), commentsPath.Value());
                        }
                    }

                    return exitCode;
                });
            });
        }
    }
}
