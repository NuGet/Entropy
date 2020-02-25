using System;

namespace FixDevV3Blobs
{
    public class UnsupportedContentEncodingException : InvalidOperationException
    {
        public UnsupportedContentEncodingException(string encoding)
            : base($"Unsupported content encoding: {encoding}")
        {
        }
    }
}
