﻿using System.Net.Http;

namespace RestoreReplay
{
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
