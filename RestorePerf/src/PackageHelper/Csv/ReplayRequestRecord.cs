namespace PackageHelper.Csv
{
    public class ReplayRequestRecord
    {
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public double HeaderDurationMs { get; set; }
        public double BodyDurationMs { get; set; }
    }
}
