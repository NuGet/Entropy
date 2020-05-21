﻿using System.Net.Http;

namespace PackageHelper.RestoreReplay
{
    class StartRequest
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
