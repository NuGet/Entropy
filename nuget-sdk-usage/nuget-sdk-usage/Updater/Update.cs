using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using nuget_sdk_usage.Analysis.Assembly;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace nuget_sdk_usage.Updater
{
    internal class Update
    {
        internal static Command GetCommand()
        {
            var command = new Command("update")
            {
                new Option<DirectoryInfo>("--results")
                {
                    Argument = new Argument<DirectoryInfo>().ExistingOnly(),
                    Required = true
                },
                new Option<DirectoryInfo>("--source")
                {
                    Argument = new Argument<DirectoryInfo>().ExistingOnly(),
                    Required = true
                }
            };

            command.Handler = CommandHandler.Create((Func<DirectoryInfo, DirectoryInfo, IConsole, Task<int>>)InvokeAsync);

            return command;
        }

        private static async Task<int> InvokeAsync(DirectoryInfo results, DirectoryInfo source, IConsole console)
        {
            bool error = false;

            if (!results.Exists)
            {
                error = true;
                console.Error.WriteLine($"Directory \"{results.FullName}\" does not exist.");
            }

            var solutionFullFileName = new FileInfo(Path.Combine(source.FullName, "NuGet.sln"));
            if (!solutionFullFileName.Exists)
            {
                error = true;
                console.Error.WriteLine($"\"{solutionFullFileName}\" does not exist.");
            }

            if (error)
            {
                return -1;
            }

            Task<Dictionary<string, HashSet<string>>> getUsageTask = GetScanResults(results);
            Task<(MSBuildWorkspace, Solution)> openSolutionTask = OpenSolutionAsync(solutionFullFileName);
            Task<Dictionary<string, HashSet<string>>> getMembersWithAttributeTask = GetMembersWithAttributeAsync(openSolutionTask);
            Task<Dictionary<string, Dictionary<string, bool>>> getDiffTask = GetDiffAsync(getUsageTask, getMembersWithAttributeTask);
            Task updateSourceTask = UpdateSourceAsync(getDiffTask, openSolutionTask);

            await Task.WhenAll(getUsageTask, openSolutionTask, getMembersWithAttributeTask, getDiffTask, updateSourceTask);

            return 0;
        }

        private static async Task UpdateSourceAsync(Task<Dictionary<string, Dictionary<string, bool>>> getDiffTask, Task<(MSBuildWorkspace, Solution)> solutionTask)
        {
            var diffResults = await getDiffTask;
            var (_, sln) = await solutionTask;

            Console.WriteLine("Starting " + nameof(UpdateSourceAsync));

            var actionsByAssembly = new Dictionary<string, Dictionary<string, UpdateAction>>();
            foreach (var (assembly, diffActions) in diffResults)
            {
                var actions = new Dictionary<string, UpdateAction>();
                foreach (var (member, add) in diffActions)
                {
                    actions.Add(member, new UpdateAction(add));
                }

                actionsByAssembly.Add(assembly, actions);
            }

            foreach (var project in sln.Projects)
            {
                var assembly = project.AssemblyName;
                if (!actionsByAssembly.TryGetValue(assembly, out var actions))
                {
                    continue;
                }

                foreach (var document in project.Documents)
                {
                    var semanticModel = await document.GetSemanticModelAsync() ?? throw new NotSupportedException("Don't know how to handle case when semantic model unavailable.");
                    var attributeUpdater = new SourceUpdater(actions, semanticModel);
                    var before = await document.GetSyntaxRootAsync() ?? throw new NotSupportedException("Don't know how to handle case when document has no syntax root");
                    var syntaxRoot = await document.GetSyntaxRootAsync();
                    var after = attributeUpdater.Visit(syntaxRoot);
                    if (!before.Equals(after))
                    {
                        var filename = document.FilePath ?? throw new NotSupportedException("A document without a file?");
                        using (var fileStream = new StreamWriter(filename, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                        {
                            after.WriteTo(fileStream);
                        }
                    }
                }
            }

            int total = 0, updated = 0;
            foreach (var (assembly, actions) in actionsByAssembly)
            {
                foreach (var (method, updateInfo) in actions)
                {
                    total++;
                    if (!updateInfo.Actioned)
                    {
                        Console.Error.WriteLine("{0} was not actioned!", method);
                    }
                    else
                    {
                        updated++;
                        // yay!
                    }
                }
            }
            Console.WriteLine("{0}/{1} updates applied.", updated, total);

            Console.WriteLine("Finishing " + nameof(UpdateSourceAsync));
        }

        private static async Task<Dictionary<string, Dictionary<string, bool>>> GetDiffAsync(Task<Dictionary<string, HashSet<string>>> usedTask, Task<Dictionary<string, HashSet<string>>> attributedTask)
        {
            var used = await usedTask;
            var attributed = await attributedTask;

            Console.WriteLine("Starting " + nameof(GetDiffAsync));
            var result = new Dictionary<string, Dictionary<string, bool>>();

            HashSet<string> newAssemblies = new HashSet<string>();
            HashSet<string> noLongerUsedAssemblies = attributed.Keys.ToHashSet();

            foreach (var (usedAssembly, usedMembers) in used)
            {
                if (attributed.TryGetValue(usedAssembly, out var attributedMembers))
                {
                    noLongerUsedAssemblies.Remove(usedAssembly);

                    var newMembers = new List<string>();
                    var noLongerUsedMembers = attributedMembers.ToHashSet();

                    foreach (var member in usedMembers)
                    {
                        if (noLongerUsedMembers.Contains(member))
                        {
                            noLongerUsedMembers.Remove(member);
                        }
                        else
                        {
                            newMembers.Add(member);
                        }
                    }

                    if (newMembers.Count > 0 || noLongerUsedMembers.Count > 0)
                    {
                        var actions = new Dictionary<string, bool>();

                        foreach (var member in newMembers)
                        {
                            actions.Add(member, true);
                        }

                        foreach (var member in noLongerUsedMembers)
                        {
                            actions.Add(member, false);
                        }

                        result.Add(usedAssembly, actions);
                    }
                }
                else
                {
                    newAssemblies.Add(usedAssembly);
                }
            }

            foreach (var assembly in newAssemblies)
            {
                var actions = new Dictionary<string, bool>();
                foreach (var member in used[assembly])
                {
                    actions.Add(member, true);
                }

                result.Add(assembly, actions);
            }

            foreach (var assembly in noLongerUsedAssemblies)
            {
                var actions = new Dictionary<string, bool>();
                foreach (var member in attributed[assembly])
                {
                    actions.Add(member, false);
                }

                result.Add(assembly, actions);
            }

            Console.WriteLine("Finishing " + nameof(GetDiffAsync));
            return result;
        }

        private static async Task<Dictionary<string, HashSet<string>>> GetMembersWithAttributeAsync(Task<(MSBuildWorkspace, Solution)> arg1)
        {
            var (_, sln) = await arg1;

            Console.WriteLine("Starting " + nameof(GetMembersWithAttributeAsync));
            var result = new Dictionary<string, HashSet<string>>();

            foreach (var project in sln.Projects)
            {
                var assembly = project.AssemblyName;

                foreach (var document in project.Documents)
                {
                    var semanticModel = await document.GetSemanticModelAsync() ?? throw new NotSupportedException("Don't know how to handle case when semantic model unavailable.");
                    var finder = new AttributeUsageFinder(semanticModel);

                    finder.Visit(await document.GetSyntaxRootAsync());

                    if (finder.FoundMembers.Count > 0)
                    {
                        if (!result.TryGetValue(assembly, out var members))
                        {
                            members = new HashSet<string>();
                            result.Add(assembly, members);
                        }

                        foreach (var member in finder.FoundMembers)
                        {
                            members.Add(member);
                        }
                    }
                }
            }

            Console.WriteLine("Finishing " + nameof(GetMembersWithAttributeAsync));
            return result;
        }

        private static async Task<Dictionary<string, HashSet<string>>> GetScanResults(DirectoryInfo resultsDirectory)
        {
            Console.WriteLine("Starting " + nameof(GetScanResults));
            var result = new Dictionary<string, HashSet<string>>();

            var resultFiles = resultsDirectory.EnumerateFiles();
            foreach (var file in resultFiles)
            {
                using (var stream = file.OpenRead())
                {
                    var usage = await JsonSerializer.DeserializeAsync<UsageInformation>(stream);

                    foreach (var assemblyKvp in usage.MemberReferences)
                    {
                        if (!result.TryGetValue(assemblyKvp.Key, out var allMembers))
                        {
                            allMembers = new HashSet<string>();
                            result.Add(assemblyKvp.Key, allMembers);
                        }

                        foreach (var member in assemblyKvp.Value)
                        {
                            allMembers.Add(member);
                        }
                    }
                }
            }

            Console.WriteLine("Finishing " + nameof(GetScanResults));
            return result;
        }

        private static async Task<(MSBuildWorkspace workspace, Solution solution)> OpenSolutionAsync(FileInfo solutionFullFileName)
        {
            Console.WriteLine("Starting " + nameof(OpenSolutionAsync));
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

            var sln = await workspace.OpenSolutionAsync(solutionFullFileName.FullName);

            Console.WriteLine("Finishing " + nameof(OpenSolutionAsync));
            return (workspace, sln);
        }

        private static void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            Console.WriteLine(e.Diagnostic.Kind + ": " + e.Diagnostic.Message);
        }
    }
}
