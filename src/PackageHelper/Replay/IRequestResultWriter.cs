using System;
using System.Net;

namespace PackageHelper.Replay
{
    interface IRequestResultWriter
    {
        void OnResponse(
            RequestNode node,
            HttpStatusCode statusCode,
            TimeSpan headerDuration,
            TimeSpan bodyDuration);
    }
}
