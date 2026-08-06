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

    [Fact]
    public void EscapesSpecialCharacters()
    {
        var json = new ProductLabelFormatter().Format("A\"B\nC", 1);

        Assert.Equal("{\"sku\":\"A\\\"B\\nC\",\"quantity\":1}", json);
    }

    [Fact]
    public void FormatsZeroQuantity()
    {
        var json = new ProductLabelFormatter().Format("EMPTY", 0);

        Assert.Equal("""{"sku":"EMPTY","quantity":0}""", json);
    }
}
