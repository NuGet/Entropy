using Xunit;
using VulnerableApp;

namespace VulnerableApp.Tests;

public class UserDataServiceTests
{
    [Fact]
    public void SerializeUser_ProducesValidJson()
    {
        var service = new UserDataService();
        var user = new User("Alice", "alice@example.com", 30);

        var json = service.SerializeUser(user);

        Assert.NotNull(json);
        Assert.Contains("Alice", json);
        Assert.Contains("alice@example.com", json);
    }

    [Fact]
    public void DeserializeUser_RoundTrips()
    {
        var service = new UserDataService();
        var user = new User("Alice", "alice@example.com", 30);

        var result = service.DeserializeUser(service.SerializeUser(user));

        Assert.NotNull(result);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Age, result.Age);
    }

    [Fact]
    public void DeserializeUser_ReturnsNull_ForNullJson()
    {
        var service = new UserDataService();

        var result = service.DeserializeUser("null");

        Assert.Null(result);
    }
}
