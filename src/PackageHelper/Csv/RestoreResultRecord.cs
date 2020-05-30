using CsvHelper.Configuration.Attributes;

namespace PackageHelper.Csv
{
    public class RestoreResultRecord
    {
        [Name("Machine Name")]
        public string MachineName { get; set; }

        [Name("Client Name")]
        public string ClientName { get; set; }

        [Name("Client Version")]
        public string ClientVersion { get; set; }

        [Name("Solution Name")]
        public string SolutionName { get; set; }

        [Name("Test Run ID")]
        public string TestRunID { get; set; }

        [Name("Iteration")]
        public int Iteration { get; set; }

        [Name("Iteration Count")]
        public int IterationCount { get; set; }

        [Name("Scenario Name")]
        public string ScenarioName { get; set; }

        [Name("Variant Name")]
        public string VariantName { get; set; }

        [Name("Total Time (seconds)")]
        public double TotalTimeSeconds { get; set; }

        [Name("Project Restore Count")]
        public int ProjectRestoreCount { get; set; }

        [Name("Max Project Restore Time (seconds)")]
        public double MaxProjectRestoreTimeSeconds { get; set; }

        [Name("Sum Project Restore Time (seconds)")]
        public double SumProjectRestoreTimeSeconds { get; set; }

        [Name("Average Project Restore Time (seconds)")]
        public double AverageProjectRestoreTimeSeconds { get; set; }

        [Name("Force")]
        public bool Force { get; set; }

        [Name("Global Packages Folder .nupkg Count")]
        public int GlobalPackagesFolderNupkgCount { get; set; }

        [Name("Global Packages Folder .nupkg Size (MB)")]
        public double GlobalPackagesFolderNupkgSizeMb { get; set; }

        [Name("Global Packages Folder File Count")]
        public int GlobalPackagesFolderFileCount { get; set; }

        [Name("Global Packages Folder File Size (MB)")]
        public double GlobalPackagesFolderFileSizeMb { get; set; }

        [Name("Clean Global Packages Folder")]
        public bool CleanGlobalPackagesFolder { get; set; }

        [Name("HTTP Cache File Count")]
        public int HttpCacheFileCount { get; set; }

        [Name("HTTP Cache File Size (MB)")]
        public double HttpCacheFileSizeMb { get; set; }

        [Name("Clean HTTP Cache")]
        public bool CleanHttpCache { get; set; }

        [Name("Plugins Cache File Count")]
        public int PluginsCacheFileCount { get; set; }

        [Name("Plugins Cache File Size (MB)")]
        public double PluginsCacheFileSizeMb { get; set; }

        [Name("Clean Plugins Cache")]
        public bool CleanPluginsCache { get; set; }

        [Name("Kill MSBuild and dotnet Processes")]
        public bool KillMSBuildAndDotnetProcesses { get; set; }

        [Name("Processor Name")]
        public string ProcessorName { get; set; }

        [Name("Processor Physical Core Count")]
        public int? ProcessorPhysicalCoreCount { get; set; }

        [Name("Processor Logical Core Count")]
        public int? ProcessorLogicalCoreCount { get; set; }

        [Name("Log File Name")]
        public string LogFileName { get; set; }
    }
}
