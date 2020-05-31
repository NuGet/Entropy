namespace PackageHelper.Csv
{
    public class TestResultRecord
    {
        public string TimestampUtc { get; set; }
        public string VariantName { get; set; }
        public string SolutionName { get; set; }
        public TestType TestType { get; set; }
        public string MachineName { get; set; }
        public int TestResultIndex { get; set; }
        public bool IsWarmUp { get; set; }
        public int Iteration { get; set; }
        public int Iterations { get; set; }
        public double DurationMs { get; set; }
        public string LogFileName { get; set; }
        public bool Dependencies { get; set; }
    }
}
