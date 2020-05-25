using System;
using System.Net;

namespace PackageHelper.Replay.Requests
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
