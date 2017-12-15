using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BuildTestCA
{
    /// <summary>
    /// Source:
    /// https://github.com/NuGet/NuGet.Client/blob/fe7b05feaa89192b4b66fa56dfd02dece90e82c5/test/TestUtilities/Test.Utility/CommandRunner.cs
    /// </summary>
    public class CommandRunner
    {
        public static CommandRunnerResult Run(
            string process,
            string workingDirectory,
            string arguments,
            bool waitForExit,
            int timeOutInMilliseconds = 60000,
            Action<StreamWriter> inputAction = null,
            bool shareProcessObject = false,
            IDictionary<string, string> environmentVariables = null)
        {
            var psi = new ProcessStartInfo(Path.GetFullPath(process), arguments)
            {
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = inputAction != null
            };

            if (environmentVariables != null)
            {
                foreach (var pair in environmentVariables)
                {
                    psi.EnvironmentVariables[pair.Key] = pair.Value;
                }
            }

            int exitCode = 1;

            var output = new StringBuilder();
            var errors = new StringBuilder();

            Process p = null;

            try
            {
                p = new Process();

                p.StartInfo = psi;
                p.Start();

                ChildProcessTracker.AddProcess(p);

                var outputTask = ConsumeStreamReaderAsync(p.StandardOutput, output);
                var errorTask = ConsumeStreamReaderAsync(p.StandardError, errors);

                inputAction?.Invoke(p.StandardInput);

                if (waitForExit)
                {
                    var processExited = p.WaitForExit(timeOutInMilliseconds);

                    if (!processExited)
                    {
                        p.Kill();

                        var processName = Path.GetFileName(process);

                        throw new TimeoutException($"{processName} timed out: " + psi.Arguments);
                    }

                    if (processExited)
                    {
                        Task.WaitAll(outputTask, errorTask);
                        exitCode = p.ExitCode;
                    }
                }
            }
            finally
            {
                if (!shareProcessObject)
                {
                    p.Dispose();
                }
            }

            return new CommandRunnerResult(p, exitCode, output.ToString(), errors.ToString());
        }

        private static async Task ConsumeStreamReaderAsync(StreamReader reader, StringBuilder lines)
        {
            await Task.Yield();

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.AppendLine(line);
            }
        }
    }
}
