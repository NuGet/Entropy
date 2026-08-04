using System.Xml.Linq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class PackagePolicyTests
{
    private static readonly XDocument Project = XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "InventoryApp.csproj"));

    [Fact]
    public void TargetFrameworkRemainsNet8()
    {
        Assert.Equal("net8.0", Project.Descendants("TargetFramework").Single().Value);
    }

    [Fact]
    public void UsesCompatibleEfCoreSqliteVersion()
    {
        AssertPackageVersion("Microsoft.EntityFrameworkCore.Sqlite", "9.0.18");
    }

    [Fact]
    public void UsesCompatibleEfCoreDesignVersion()
    {
        AssertPackageVersion("Microsoft.EntityFrameworkCore.Design", "9.0.18");
    }

    [Fact]
    public void UsesCompatibleLoggingConsoleVersion()
    {
        AssertPackageVersion("Microsoft.Extensions.Logging.Console", "10.0.10");
    }

    private static void AssertPackageVersion(string packageName, string expectedVersion)
    {
        var package = Project.Descendants("PackageReference")
            .Single(reference => (string?)reference.Attribute("Include") == packageName);
        Assert.Equal(expectedVersion, (string?)package.Attribute("Version"));
    }
}
