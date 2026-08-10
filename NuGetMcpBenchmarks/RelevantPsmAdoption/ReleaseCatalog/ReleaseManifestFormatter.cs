using Newtonsoft.Json;

namespace ReleaseCatalog;

public sealed class ReleaseManifestFormatter
{
    public string Format(string product, string version, bool supported)
    {
        return JsonConvert.SerializeObject(new { product, version, supported });
    }
}
