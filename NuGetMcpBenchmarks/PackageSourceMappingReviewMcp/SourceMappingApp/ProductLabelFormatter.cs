using Newtonsoft.Json;

namespace SourceMappingApp;

public sealed class ProductLabelFormatter
{
    public string Format(string sku, int quantity)
    {
        return JsonConvert.SerializeObject(new { sku, quantity });
    }
}
