using Newtonsoft.Json;

namespace Contoso.Internal.Core;

public static class JsonHelper
{
    public static string ToJson(object value) => JsonConvert.SerializeObject(value);

    public static T? FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json);
}
