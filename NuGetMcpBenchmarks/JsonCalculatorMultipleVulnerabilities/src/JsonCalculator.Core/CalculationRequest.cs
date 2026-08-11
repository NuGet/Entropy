using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonCalculator.Core;

public sealed class CalculationRequest
{
    public string Operation { get; set; } = string.Empty;

    public decimal Left { get; set; }

    public decimal Right { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement> Metadata { get; set; } =
        new Dictionary<string, JsonElement>();
}
