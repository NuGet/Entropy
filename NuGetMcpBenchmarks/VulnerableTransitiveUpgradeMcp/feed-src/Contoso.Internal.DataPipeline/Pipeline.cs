using Contoso.Internal.Core;

namespace Contoso.Internal.DataPipeline;

public sealed class Pipeline
{
    public string Serialize(object payload) => JsonHelper.ToJson(payload);

    public T? Deserialize<T>(string json) => JsonHelper.FromJson<T>(json);
}
