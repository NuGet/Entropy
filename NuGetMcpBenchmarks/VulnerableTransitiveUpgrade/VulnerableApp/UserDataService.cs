using Contoso.Internal.DataPipeline;

namespace VulnerableApp;

public class UserDataService
{
    private readonly Pipeline _pipeline = new();

    public string SerializeUser(User user) => _pipeline.Serialize(user);

    public User? DeserializeUser(string json) => _pipeline.Deserialize<User>(json);
}

public record User(string Name, string Email, int Age);
