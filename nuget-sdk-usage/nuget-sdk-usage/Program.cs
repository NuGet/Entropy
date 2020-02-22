using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace nuget_sdk_usage
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            if (!TryGetSearchPath(args, out var path))
            {
                return;
            }

            var files =
                Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories));

            var usedApis = await GetUsageInformation(files);
            WriteApiList(usedApis);
        }

        static bool TryGetSearchPath(string[] args, [NotNullWhen(true)] out string? path)
        {
            path = null;

            if (args.Length == 0)
            {
                path = Environment.CurrentDirectory;
                return true;
            }
            else if (args.Length == 1)
            {
                path = args[0];
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Directory \"" + path + "\" does not exist");
                    return false;
                }
                return true;
            }
            else
            {

                Console.WriteLine("Only one path expected. Found " + args.Length);
                return false;
            }
        }

        private static async Task<UsageInformation> GetUsageInformation(IEnumerable<string> files)
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

            var getApisBlock = new TransformBlock<string, UsageInformation?>(GetApisUsedByAssembly, multipleInstancesOption);
            var collectApisBlock = new ActionBlock<UsageInformation?>(assemblyInfo =>
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
                        if (!result.MemberReferences.TryGetValue(apisByAssembly.Key, out HashSet<string>? allApis))
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

        private static UsageInformation? GetApisUsedByAssembly(string file)
        {
            // Skip NuGet's own assemblies
            string filename = Path.GetFileNameWithoutExtension(file);
            if (NuGetAssembly.MatchesName(filename)
                || filename.Equals("NuGet.Core", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (filename.StartsWith("NuGet.", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Suspicious assembly name: " + file);
            }

            using var fileStream = File.OpenRead(file);
            using var peReader= new PEReader(fileStream);

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

        private static void WriteApiList(UsageInformation apis)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(apis, options);
            Console.WriteLine(json);
        }
    }
}
