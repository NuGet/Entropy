using PackageHelper.Replay.Operations;

namespace PackageHelper.Csv
{
    public class RequestDurationRecord
    {
        public string VariantName { get; set; }
        public string SolutionName { get; set; }
        public RequestType RequestType { get; set; }
        public string MachineName { get; set; }
        public int LogFileIndex { get; set; }
        public int LogFileRequestIndex { get; set; }
        public bool? IsWarmUp { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public double HeaderDurationMs { get; set; }
        public double? BodyDurationMs { get; set; }
        public OperationType? OperationType { get; set; } 
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
    }
}
