namespace NuGet.TeamCity.AgentAuthorizer
{
    public enum ApplicationResult
    {
        InvalidArguments,
        Timeout,
        NoMatchingAgentPool,
        Success
    }
}
