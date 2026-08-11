using System.Text.Json;
using JsonCalculator.Core;

namespace JsonCalculator.Tests;

public sealed class JsonCalculationSerializerTests
{
    [Fact]
    public void DeserializeRequest_RetainsExtensionMetadata()
    {
        const string json =
            """{"operation":"multiply","left":6,"right":7,"requestId":"sample-42"}""";
        var serializer = new JsonCalculationSerializer();

        CalculationRequest request = serializer.DeserializeRequest(json);

        Assert.Equal("multiply", request.Operation);
        Assert.Equal(6, request.Left);
        Assert.Equal(7, request.Right);
        Assert.Equal("sample-42", request.Metadata["requestId"].GetString());
    }

    [Fact]
    public void SerializeResult_UsesPublicJsonContract()
    {
        var serializer = new JsonCalculationSerializer();
        var result = new CalculationResult("add", 2, 3, 5);

        string json = serializer.SerializeResult(result);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.Equal("add", document.RootElement.GetProperty("operation").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("left").GetDecimal());
        Assert.Equal(3, document.RootElement.GetProperty("right").GetDecimal());
        Assert.Equal(5, document.RootElement.GetProperty("value").GetDecimal());
    }
}
