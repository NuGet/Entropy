using Newtonsoft.Json;

namespace FeedCatalog;

public sealed class CatalogEntryFormatter
{
    public string Format(string id, string channel, bool supported)
    {
        return JsonConvert.SerializeObject(new { id, channel, supported });
    }
}
