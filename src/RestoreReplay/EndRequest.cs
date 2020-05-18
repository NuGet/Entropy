using System;
using System.Net;

namespace RestoreReplay
{
    public class EndRequest
    {
        public EndRequest(HttpStatusCode statusCode, string url, TimeSpan duration)
        {
            StatusCode = statusCode;
            Url = url;
            Duration = duration;
        }

        public HttpStatusCode StatusCode { get; }
        public string Url { get; }
        public TimeSpan Duration { get; }
    }
}
