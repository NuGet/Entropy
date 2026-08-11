using System;
using System.Globalization;
using System.Threading;
using JsonCalculator.Core;
using Microsoft.Extensions.Caching.Memory;

namespace JsonCalculator.Api;

public sealed class CachedCalculator
{
    private readonly IMemoryCache _cache;
    private readonly CalculatorEngine _engine;
    private int _cacheMissCount;

    public CachedCalculator(IMemoryCache cache, CalculatorEngine engine)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public int CacheMissCount => _cacheMissCount;

    public CalculationResult Calculate(CalculationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string key = string.Join(
            ":",
            request.Operation.Trim().ToLowerInvariant(),
            request.Left.ToString(CultureInfo.InvariantCulture),
            request.Right.ToString(CultureInfo.InvariantCulture));

        if (_cache.TryGetValue(key, out CalculationResult? cached) && cached is not null)
        {
            return cached;
        }

        CalculationResult result = _engine.Calculate(request);
        Interlocked.Increment(ref _cacheMissCount);
        _cache.Set(key, result);
        return result;
    }
}
