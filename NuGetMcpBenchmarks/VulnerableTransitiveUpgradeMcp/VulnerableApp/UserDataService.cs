using Newtonsoft.Json;

namespace VulnerableApp;

public class UserDataService
{
    public string SerializeUser(User user) => JsonConvert.SerializeObject(user);

    public User? DeserializeUser(string json) => JsonConvert.DeserializeObject<User>(json);
}

public record User(string Name, string Email, int Age);
