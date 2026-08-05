using Xunit;

namespace SourceMappingApp.Tests;

public sealed class ProductLabelFormatterTests
{
    [Fact]
    public void FormatsProductAsJson()
    {
        var json = new ProductLabelFormatter().Format("WIDGET-42", 7);

        Assert.Equal("""{"sku":"WIDGET-42","quantity":7}""", json);
    }
}
