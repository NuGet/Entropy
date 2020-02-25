using System;

namespace TestIngestionPerf
{
    public class EndpointResult
    {
        public EndpointResult(IEndpointChecker endpointChecker, TimeSpan duration)
        {
            EndpointChecker = endpointChecker;
            Duration = duration;
        }

        public IEndpointChecker EndpointChecker { get; }
        public TimeSpan Duration { get; }
    }       
}
