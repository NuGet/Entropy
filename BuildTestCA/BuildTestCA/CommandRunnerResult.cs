using System.Diagnostics;

namespace BuildTestCA
{
    /// <summary>
    /// Source:
    /// https://github.com/NuGet/NuGet.Client/blob/fe7b05feaa89192b4b66fa56dfd02dece90e82c5/test/TestUtilities/Test.Utility/CommandRunnerResult.cs
    /// </summary>
    public class CommandRunnerResult
    {
        public Process Process { get; }
        public int ExitCode { get; }
        public string Output { get; }
        public string Error { get; }

        internal CommandRunnerResult(Process process, int exitCode, string output, string error)
        {
            Process = process;
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }
    }
}
