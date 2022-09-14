namespace NuGet.GithubEventHandler
{
    public class Environment : IEnvironment
    {
        public string? Get(string name) => System.Environment.GetEnvironmentVariable(name);
    }
}
