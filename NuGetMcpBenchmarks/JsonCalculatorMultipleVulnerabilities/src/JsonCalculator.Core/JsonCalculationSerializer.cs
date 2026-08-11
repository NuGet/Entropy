using System;
using System.Text.Json;

namespace JsonCalculator.Core;

public sealed class JsonCalculationSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CalculationRequest DeserializeRequest(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        return JsonSerializer.Deserialize<CalculationRequest>(json, Options)
            ?? throw new JsonException("The request did not contain a JSON object.");
    }

    public string SerializeResult(CalculationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return JsonSerializer.Serialize(result, Options);
    }
}
