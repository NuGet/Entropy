using System.Collections.Generic;

namespace nuget_sdk_usage
{
    internal static class KeyValuePairExtensions
    {
        // Only needed while project targets netfx. If we can target netcoreapp3.1, this would be built-in.
        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
