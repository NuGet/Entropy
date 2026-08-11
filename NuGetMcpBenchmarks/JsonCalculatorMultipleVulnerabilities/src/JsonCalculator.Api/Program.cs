using JsonCalculator.Api;
using JsonCalculator.Core;
using JsonCalculator.Legacy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CalculatorEngine>();
builder.Services.AddSingleton<CachedCalculator>();
builder.Services.AddSingleton<LegacyHistoryImporter>();

var app = builder.Build();

app.MapPost(
    "/calculate",
    (CalculationRequest request, CachedCalculator calculator) => calculator.Calculate(request));
app.MapPost(
    "/history/import",
    (LegacyHistoryPayload payload, LegacyHistoryImporter importer) => importer.Import(payload.Json));

app.Run();

public sealed class LegacyHistoryPayload
{
    public string Json { get; set; } = string.Empty;
}
