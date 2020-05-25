using System.Diagnostics;

namespace PackageHelper.Replay
{
    [DebuggerDisplay("{Method,nq} {Url,nq}")]
    public class StartRequest
    {
        public StartRequest(string method, string url)
        {
            Method = method;
            Url = url;
        }

        public string Method { get; }
        public string Url { get; }
    }
}
