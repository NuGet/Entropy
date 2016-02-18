using System;

namespace LogReplay.LogParsing
{
    public class W3CLogEntry
    {
        public DateTimeOffset RequestDateTime { get; set; }
        public string SiteName { get; set; }
        public string HttpMethod { get; set; }
        public string RequestPath { get; set; }
        public string QueryString { get; set; }
        public int? Port { get; set; }
        public string UserName { get; set; }
        public string ClientIpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Cookie { get; set; }
        public string Referrer { get; set; }
        public string ClientHost { get; set; }
        public int? StatusCode { get; set; }
        public int? SubStatusCode { get; set; }
        public string Win32Status { get; set; }
        public long? BytesSent { get; set; }
        public long? BytesReceived { get; set; }
        public int? TimeTaken { get; set; }
    }
}