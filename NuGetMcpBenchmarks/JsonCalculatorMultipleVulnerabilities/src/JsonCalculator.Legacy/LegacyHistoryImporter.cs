using System;
using System.Collections.Generic;
using JsonCalculator.Core;
using Newtonsoft.Json;

namespace JsonCalculator.Legacy;

public sealed class LegacyHistoryImporter
{
    private readonly CalculatorEngine _engine;

    public LegacyHistoryImporter(CalculatorEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public IReadOnlyList<CalculationResult> Import(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        LegacyHistoryDocument document = JsonConvert.DeserializeObject<LegacyHistoryDocument>(json)
            ?? throw new JsonSerializationException("The history did not contain a JSON object.");
        var results = new List<CalculationResult>();

        foreach (LegacyHistoryEntry entry in document.Entries)
        {
            results.Add(_engine.Calculate(new CalculationRequest
            {
                Operation = entry.Operation,
                Left = entry.Left,
                Right = entry.Right
            }));
        }

        return results;
    }

    private sealed class LegacyHistoryDocument
    {
        [JsonProperty("entries")]
        public List<LegacyHistoryEntry> Entries { get; set; } = new List<LegacyHistoryEntry>();
    }

    private sealed class LegacyHistoryEntry
    {
        [JsonProperty("op")]
        public string Operation { get; set; } = string.Empty;

        [JsonProperty("a")]
        public decimal Left { get; set; }

        [JsonProperty("b")]
        public decimal Right { get; set; }
    }
}
