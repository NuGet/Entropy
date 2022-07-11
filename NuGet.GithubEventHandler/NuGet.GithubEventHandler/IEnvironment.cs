namespace NuGet.GithubEventHandler
{
    public interface IEnvironment
    {
        string? Get(string name);
    }
}
