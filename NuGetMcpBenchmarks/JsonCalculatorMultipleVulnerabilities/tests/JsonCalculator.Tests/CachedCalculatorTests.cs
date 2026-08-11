using JsonCalculator.Api;
using JsonCalculator.Core;
using Microsoft.Extensions.Caching.Memory;

namespace JsonCalculator.Tests;

public sealed class CachedCalculatorTests
{
    [Fact]
    public void Calculate_ReusesResultForEquivalentRequest()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var calculator = new CachedCalculator(cache, new CalculatorEngine());
        var request = new CalculationRequest
        {
            Operation = "multiply",
            Left = 6,
            Right = 7
        };

        CalculationResult first = calculator.Calculate(request);
        CalculationResult second = calculator.Calculate(request);

        Assert.Same(first, second);
        Assert.Equal(42, second.Value);
        Assert.Equal(1, calculator.CacheMissCount);
    }
}
