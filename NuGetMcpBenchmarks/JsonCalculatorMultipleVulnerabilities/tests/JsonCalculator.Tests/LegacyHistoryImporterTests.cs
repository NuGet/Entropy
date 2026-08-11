using JsonCalculator.Core;
using JsonCalculator.Legacy;

namespace JsonCalculator.Tests;

public sealed class LegacyHistoryImporterTests
{
    [Fact]
    public void Import_ConvertsLegacyEntriesToCurrentResults()
    {
        const string json =
            """
            {
              "entries": [
                { "op": "add", "a": 2, "b": 3 },
                { "op": "multiply", "a": 6, "b": 7 }
              ]
            }
            """;
        var importer = new LegacyHistoryImporter(new CalculatorEngine());

        IReadOnlyList<CalculationResult> results = importer.Import(json);

        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal("add", result.Operation);
                Assert.Equal(5, result.Value);
            },
            result =>
            {
                Assert.Equal("multiply", result.Operation);
                Assert.Equal(42, result.Value);
            });
    }
}
