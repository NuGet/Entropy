namespace PackageHelper.Csv
{
    public class ReplayResultRecord
    {
        public string TimestampUtc { get; set; }
        public string MachineName { get; set; }
        public int Iteration { get; set; }
        public bool IsWarmUp { get; set; }
        public int Iterations { get; set; }
        public string VariantName { get; set; }
        public string SolutionName { get; set; }
        public object RequestCount { get; set; }
        public double DurationMs { get; set; }
        public int MaxConcurrency { get; set; }
        public string LogFileName { get; set; }
        public bool NoDependencies { get; set; }
    }
}
