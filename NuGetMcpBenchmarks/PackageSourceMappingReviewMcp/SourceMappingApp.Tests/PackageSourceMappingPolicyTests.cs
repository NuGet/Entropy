using System.Xml.Linq;
using Xunit;

namespace SourceMappingApp.Tests;

public sealed class PackageSourceMappingPolicyTests
{
    private static readonly XDocument Config = XDocument.Load(
        Path.Combine(AppContext.BaseDirectory, "Repository.NuGet.Config"));

    [Fact]
    public void RepositoryConfigClearsInheritedPackageSources()
    {
        AssertClearIsFirst(Config.Root!.Element("packageSources"));
    }

    [Fact]
    public void RepositoryConfigDefinesOnlyNugetOrg()
    {
        var sources = Config.Root!.Element("packageSources")!
            .Elements("add")
            .Select(element => (
                Key: (string?)element.Attribute("key"),
                Value: (string?)element.Attribute("value")))
            .ToArray();

        Assert.Equal(
            [("nuget.org", "https://api.nuget.org/v3/index.json")],
            sources);
    }

    [Fact]
    public void RepositoryConfigClearsInheritedPackageSourceMappings()
    {
        AssertClearIsFirst(Config.Root!.Element("packageSourceMapping"));
    }

    [Fact]
    public void RepositoryConfigMapsEveryPackageToNugetOrg()
    {
        var section = Config.Root!.Element("packageSourceMapping");
        Assert.NotNull(section);

        var mappings = section!
            .Elements("packageSource")
            .ToArray();

        var mapping = Assert.Single(mappings);
        Assert.Equal("nuget.org", (string?)mapping.Attribute("key"));
        Assert.Equal(
            [
                "Microsoft.*",
                "NETStandard.Library",
                "Newtonsoft.Json",
                "NuGet.*",
                "runtime.*",
                "System.*",
                "xunit*"
            ],
            mapping.Elements("package")
                .Select(element => (string?)element.Attribute("pattern"))
                .Order());
    }

    [Fact]
    public void ReviewExplainsAllChoicesAndRecordsAdoptDecision()
    {
        var reviewPath = Path.Combine(AppContext.BaseDirectory, "PACKAGE_SOURCE_MAPPING_REVIEW.md");
        Assert.True(File.Exists(reviewPath), "Create PACKAGE_SOURCE_MAPPING_REVIEW.md.");

        var review = File.ReadAllText(reviewPath);
        Assert.Contains("Adopt", review, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Disable", review, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Keep", review, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Decision: Adopt", review, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inherited", review, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("portable", review, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertClearIsFirst(XElement? section)
    {
        Assert.NotNull(section);
        Assert.Equal("clear", section!.Elements().FirstOrDefault()?.Name.LocalName);
    }
}
