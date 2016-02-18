using System;
using System.Collections;
using System.Collections.Generic;
using CommandLine;

namespace LogReplay.Commands
{
    [Verb("run", HelpText = "Replays Antares IIS logs against a different target.")]
    public class RunOptions
    {
        [Option("connectionString", Required = true, HelpText = "Azure Storage connection string.")]
        public string ConnectionString { get; set; }

        [Option("logContainer", Required = true, HelpText = "Log container.")]
        public string LogContainer { get; set; }

        [Option("logContainerRoot", Required = false, HelpText = "Log container root directory.")]
        public string LogContainerRoot { get; set; }

        [Option("fromPrefix", Required = true, HelpText = "Replay log starting from this blob directory prefix. Example: 2016/02/18/07", Separator = '/')]
        public IEnumerable<string> FromPrefix { get; set; }

        [Option("untilPrefix", Required = true, HelpText = "Replay log until this blob directory prefix. Example: 2016/02/18/08", Separator = '/')]
        public IEnumerable<string> UntilPrefix { get; set; }

        [Option("target", Required = true, HelpText = "Target root URL.")]
        public Uri Target { get; set; }

        [Option("logFile", Required = false, HelpText = "Replay log file path. File will be appended to.", Default = "requests.csv")]
        public string LogFile { get; set; }
    }
}