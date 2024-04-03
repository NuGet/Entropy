using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace RestoreTraceParser
{
    public static class PhaseTracker
    {
        private const string ProcessName = "NuGet.Build.Tasks.Console";
        private const string DisableParallelismArgument = "DisableParallel=True";
        private const string EventSourceName = "Microsoft-NuGet";
        private static Dictionary<string, PhaseProject> _phaseList = new Dictionary<string, PhaseProject>();
        private static Dictionary<string, PhaseProject> _directoryToProject = new Dictionary<string, PhaseProject>();
        private static Dictionary<int, PhaseProject> _threadToProject = new Dictionary<int, PhaseProject>();

        private static int _processId;
        private static double _processStartTimeStamp;
        private static double _processStopTimeStamp;
        private static double _lastNuGetEventTimeStamp;

        private static UnTrackedTimeTracker _untrackedTimeTracker;

        public static void Execute(string[] args)
        {
            string pathToTrace = args[1];
            ProcessTrace(pathToTrace);
            string outputPath = Path.Combine(Path.GetDirectoryName(pathToTrace), "restorePhases.csv");
            using (TextWriter writer = new StreamWriter(outputPath))
            {
                WriteResults(writer);
            }

            Console.WriteLine($"Process Wall Clock (ms): {(_processStopTimeStamp - _processStartTimeStamp).ToString("#.##")}");
            Console.WriteLine($"Time from last NuGet event: {(_processStopTimeStamp - _lastNuGetEventTimeStamp).ToString("#.##")}");

            if (_untrackedTimeTracker != null)
            {
                Console.WriteLine("Total Untracked Time: (ms) " + _untrackedTimeTracker.TotalUntrackedTime.ToString("#.##"));
                _untrackedTimeTracker.WriteRanges(Console.Out);
            }

            Console.WriteLine("Phase timing written to " + outputPath);
        }

        private static void ProcessTrace(string pathToTrace)
        {
            using (ETWTraceEventSource source = new ETWTraceEventSource(pathToTrace))
            {
                source.Kernel.ProcessStart += delegate (ProcessTraceData data)
                {
                    if (ProcessName.Equals(data.ProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        _processStartTimeStamp = data.TimeStampRelativeMSec;
                        _processId = data.ProcessID;
                        if(data.CommandLine.Contains(DisableParallelismArgument))
                        {
                            _untrackedTimeTracker = new UnTrackedTimeTracker();
                        }
                    }
                };

                source.Kernel.ProcessStop += delegate (ProcessTraceData data)
                {
                    if (data.ProcessID == _processId)
                    {
                        _processStopTimeStamp = data.TimeStampRelativeMSec;
                    }
                };

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreRunner/RestoreProject/Start", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    PhaseProject project = new PhaseProject();
                    _phaseList.Add(projectName, project);
                    _directoryToProject.Add(ProjectDirectoryFromProjectPath(projectName), project);
                    project.OnRestoreProjectStart(data);

                    _untrackedTimeTracker?.OnUnTrackedTimeStop(data.TimeStampRelativeMSec, "Non-Restore Time");
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreRunner/RestoreProject/Stop", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnRestoreProjectStop(data);
                    }

                    _untrackedTimeTracker?.OnUnTrackedTimeStart(data.TimeStampRelativeMSec);
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreCommand/CalcNoOpRestore/Start", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        _threadToProject[data.ThreadID] = value;
                        value.OnCalculateAndWriteDependencySpecStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreCommand/CalcNoOpRestore/Stop", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnCalculateAndWriteDependencySpecStop(data);
                    }

                    // Clear the thread mapping to PhaseProject.
                    if (_threadToProject.TryGetValue(data.ThreadID, out PhaseProject project))
                    {
                        Debug.Assert(project == value);
                        _threadToProject.Remove(data.ThreadID);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreCommand/BuildRestoreGraph/Start", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnCreateRestoreGraphStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreCommand/BuildRestoreGraph/Stop", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnCreateRestoreGraphStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreCommand/BuildAssetsFile/Start", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnBuildAssetsFileStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreCommand/BuildAssetsFile/Stop", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnBuildAssetsFileStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreRunner/CommitAsync/Start", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnCommitAsyncStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreRunner/CommitAsync/Stop", (data) =>
                {
                    string projectName = data.PayloadString(0);

                    if (_phaseList.TryGetValue(projectName, out PhaseProject value))
                    {
                        value.OnCommitAsyncStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WriteCacheFile/Start", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWriteCacheFileStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WriteCacheFile/Stop", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWriteCacheFileStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WriteDgSpecFile/Start", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWriteDgSpecFileStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WriteDgSpecFile/Stop", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWriteDgSpecFileStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WriteAssetsFile/Start", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWriteLockFileStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WriteAssetsFile/Stop", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWriteLockFileStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WritePackagesLockFile/Start", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWritePackagesLockFileStart(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, "RestoreResult/WritePackagesLockFile/Stop", (data) =>
                {
                    string filePath = data.PayloadString(0);
                    string projectPath = ProjectDirectoryFromOutputFile(filePath);

                    if (_directoryToProject.TryGetValue(projectPath, out PhaseProject value))
                    {
                        value.OnWritePackagesLockFileStop(data);
                    }
                });

                source.Dynamic.AddCallbackForProviderEvent(EventSourceName, null, (data) =>
                {
                    _lastNuGetEventTimeStamp = data.TimeStampRelativeMSec;
                });

                source.Process();
            }
        }

        private static string ProjectDirectoryFromProjectPath(string projectPath)
        {
            return Path.GetDirectoryName(projectPath);
        }

        private static string ProjectDirectoryFromOutputFile(string outputFilePath)
        {
            string directoryName = Path.GetDirectoryName(outputFilePath);
            string directoryNameOnly = Path.GetFileName(directoryName);
            if(directoryNameOnly.Equals(".pkgrefgen", StringComparison.OrdinalIgnoreCase))
            {
                directoryName = Path.GetDirectoryName(directoryName);
            }
            return directoryName;
        }

        private static void WriteResults(TextWriter writer)
        {
            writer.WriteCsvCell("Project");
            writer.WriteCsvCell("Restore Time (ms)");
            writer.WriteCsvCell("DependencySpec Time (ms)");
            writer.WriteCsvCell("CreateRestoreGraph Time (ms)");
            writer.WriteCsvCell("BuildAssetsFile Time (ms)");
            writer.WriteCsvCell("CommitAsync Time (ms)");
            writer.WriteCsvCell("CacheFile Write Time (ms)");
            writer.WriteCsvCell("DgSpecFile Write Time (ms)");
            writer.WriteCsvCell("LockFile Write Time (ms)");
            writer.WriteCsvCell("PackagesLockFile Write Time (ms)");
            writer.WriteLine();

            foreach (KeyValuePair<string, PhaseProject> pair in _phaseList)
            {
                writer.WriteCsvCell(pair.Key);
                writer.WriteCsvCell(pair.Value.RestoreTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.CalculateAndWriteDependencySpecTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.CreateRestoreGraphTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.BuildAssetsFileTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.CommitAsyncTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.WriteCacheFileTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.WriteDgSpecFileTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.WriteLockFileTime.ToString("#.##"));
                writer.WriteCsvCell(pair.Value.WritePackagesLockFileTime.ToString("#.####"));
                writer.WriteLine();
            }
        }
    }
}