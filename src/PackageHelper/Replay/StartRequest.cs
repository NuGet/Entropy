using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PackageHelper.Replay
{
    [DebuggerDisplay("{Method,nq} {Url,nq}")]
    public class StartRequest : IEquatable<StartRequest>
    {
        public StartRequest(string method, string url)
        {
            Method = method;
            Url = url;
        }

        public string Method { get; }
        public string Url { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as StartRequest);
        }

        public bool Equals(StartRequest other)
        {
            return other != null &&
                   Method == other.Method &&
                   Url == other.Url;
        }

        public override int GetHashCode()
        {
#if NETCOREAPP
            return HashCode.Combine(Method, Url);
#else
            var hashCode = 17;
            hashCode = hashCode * 31 + Method.GetHashCode();
            hashCode = hashCode * 31 + Url.GetHashCode();
            return hashCode;
#endif
        }
    }
}
