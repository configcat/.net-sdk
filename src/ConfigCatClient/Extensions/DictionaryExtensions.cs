using System.Linq;

namespace System.Collections.Generic;

internal static class DictionaryExtensions
{
    public static IReadOnlyCollection<TKey> KeyCollection<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        return
            // NOTE: It's worth special-casing Dictionary<TKey, TValue> for performance since it will almost always be
            // the underlying type in our use cases.
            source is Dictionary<TKey, TValue> dictionary ? dictionary.Keys
            : source.Keys is IReadOnlyCollection<TKey> keyCollection ? keyCollection
            : source.Keys.ToArray();
    }

    public static Dictionary<TKey, TValue> MergeOverwriteWith<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? source, IReadOnlyDictionary<TKey, TValue>? other)
        where TKey : notnull
    {
        Dictionary<TKey, TValue> result;

        if (source is { Count: > 0 })
        {
            result = new Dictionary<TKey, TValue>(capacity: source.Count);
            foreach (var item in source)
                result[item.Key] = item.Value;
        }
        else
        {
            result = new Dictionary<TKey, TValue>();
        }

        if (other is { Count: > 0 })
        {
            foreach (var item in other)
                result[item.Key] = item.Value;
        }

        return result;
    }
}
