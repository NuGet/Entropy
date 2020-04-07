using nuget_sdk_usage.Analysis.Assembly;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace nuget_sdk_usage.Analysis.Scanning
{
    internal class Scanner
    {
        internal static Command GetCommand()
        {
            var command = new Command("scan")
            {
                new Argument<DirectoryInfo>("directory")
                {
                    Arity = ArgumentArity.ExactlyOne,
                    Description = "The directory to scan"
                },
                new Option<FileInfo>("--output")
            };

            command.Handler = CommandHandler.Create(typeof(Scanner).GetMethod(nameof(InvokeAsync)));

            return command;
        }

        public async Task InvokeAsync(DirectoryInfo directory, FileInfo output)
        {
            if (!directory.Exists)
            {
                Console.WriteLine("Directory \"" + directory.FullName + "\" does not exist");
                return;
            }

            FileStream fileStream;
            TextWriter textWriter;
            if (output != null)
            {
                fileStream = output.OpenWrite();
                textWriter = new StreamWriter(fileStream, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4 * 1024, leaveOpen: true);
            }
            else
            {
                fileStream = null;
                textWriter = Console.Out;
            }

            var files =
                directory.EnumerateFiles("*.dll", SearchOption.AllDirectories)
                .Concat(directory.EnumerateFiles("*.exe", SearchOption.AllDirectories));

            var usedApis = await GetUsageInformation(files);

            if (fileStream != null)
            {
                fileStream.Position = 0;
            }
            WriteApiList(usedApis, textWriter);
            textWriter.Flush();
            textWriter.Dispose();
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.SetLength(fileStream.Position);
                fileStream.Dispose();
            }
        }

        private static async Task<UsageInformation> GetUsageInformation(IEnumerable<FileInfo> files)
        {
            var result = new UsageInformation();

            var multipleInstancesOption = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            var singleInstanceOption = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1
            };

            var getApisBlock = new TransformBlock<FileInfo, UsageInformation>(GetApisUsedByAssembly, multipleInstancesOption);
            var collectApisBlock = new ActionBlock<UsageInformation>(assemblyInfo =>
            {
                if (assemblyInfo != null)
                {
                    foreach (var targetFramework in assemblyInfo.TargetFrameworks)
                    {
                        result.TargetFrameworks.Add(targetFramework);
                    }

                    foreach (var version in assemblyInfo.Versions)
                    {
                        result.Versions.Add(version);
                    }

                    foreach (var apisByAssembly in assemblyInfo.MemberReferences)
                    {
                        if (!result.MemberReferences.TryGetValue(apisByAssembly.Key, out HashSet<string> allApis))
                        {
                            allApis = new HashSet<string>();
                            result.MemberReferences[apisByAssembly.Key] = allApis;
                        }

                        foreach (var api in apisByAssembly.Value)
                        {
                            allApis.Add(api);
                        }
                    }
                }
            },
            singleInstanceOption);

            var linkOptions = new DataflowLinkOptions()
            {
                PropagateCompletion = true
            };
            getApisBlock.LinkTo(collectApisBlock, linkOptions);

            foreach (var file in files)
            {
                await getApisBlock.SendAsync(file);
            }

            getApisBlock.Complete();

            await Task.WhenAll(getApisBlock.Completion, collectApisBlock.Completion);

            return result;
        }

        private static UsageInformation GetApisUsedByAssembly(FileInfo file)
        {
            // Skip NuGet's own assemblies
            string filename = Path.GetFileNameWithoutExtension(file.Name);
            if (NuGetAssembly.MatchesName(filename)
                || filename.Equals("NuGet.Core", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (filename.StartsWith("NuGet.", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Suspicious assembly name: " + file);
            }

            using (var fileStream = file.OpenRead())
            using (var peReader = new PEReader(fileStream))
            {
                MetadataReader metadata;
                try
                {
                    metadata = peReader.GetMetadataReader();
                }
                catch (InvalidOperationException)
                {
                    // Not a .NET assembly
                    return null;
                }

                if (!AssemblyAnalyser.HasReferenceToNuGetAssembly(metadata))
                {
                    return null;
                }

                UsageInformation usedNuGetApis = AssemblyAnalyser.FindUsedNuGetApis(metadata);

                return usedNuGetApis;
            }
        }

        private static void WriteApiList(UsageInformation apis, TextWriter output)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(apis, options);
            output.WriteLine(json);
        }
    }
}
