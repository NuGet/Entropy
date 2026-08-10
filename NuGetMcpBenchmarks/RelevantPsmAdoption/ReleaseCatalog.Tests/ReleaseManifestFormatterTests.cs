using Xunit;

namespace ReleaseCatalog.Tests;

public sealed class ReleaseManifestFormatterTests
{
    [Fact]
    public void FormatsSupportedRelease()
    {
        var json = new ReleaseManifestFormatter().Format("Client SDK", "10.1", true);

        Assert.Equal("""{"product":"Client SDK","version":"10.1","supported":true}""", json);
    }

    [Fact]
    public void EscapesManifestValues()
    {
        var json = new ReleaseManifestFormatter().Format("A\"B", "daily\nbuild", false);

        Assert.Equal("{\"product\":\"A\\\"B\",\"version\":\"daily\\nbuild\",\"supported\":false}", json);
    }

    [Fact]
    public void PreservesEmptyVersion()
    {
        var json = new ReleaseManifestFormatter().Format("CLI", string.Empty, true);

        Assert.Equal("""{"product":"CLI","version":"","supported":true}""", json);
    }
}
