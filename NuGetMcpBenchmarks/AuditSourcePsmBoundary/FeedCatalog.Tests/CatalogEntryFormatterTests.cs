using Xunit;

namespace FeedCatalog.Tests;

public sealed class CatalogEntryFormatterTests
{
    [Fact]
    public void FormatsSupportedEntry()
    {
        var json = new CatalogEntryFormatter().Format("SDK-10", "servicing", true);

        Assert.Equal("""{"id":"SDK-10","channel":"servicing","supported":true}""", json);
    }

    [Fact]
    public void EscapesEntryValues()
    {
        var json = new CatalogEntryFormatter().Format("A\"B", "daily\nbuild", false);

        Assert.Equal("{\"id\":\"A\\\"B\",\"channel\":\"daily\\nbuild\",\"supported\":false}", json);
    }

    [Fact]
    public void PreservesEmptyChannel()
    {
        var json = new CatalogEntryFormatter().Format("SDK-11", string.Empty, true);

        Assert.Equal("""{"id":"SDK-11","channel":"","supported":true}""", json);
    }
}
