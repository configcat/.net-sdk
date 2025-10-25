using System.Collections.ObjectModel;

namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    public static IReadOnlyList<T> AsReadOnly<T>(this T[]? source)
    {
        return source is { Length: > 0 } ? new ReadOnlyCollection<T>(source) : Array.Empty<T>();
    }

    public static Dictionary<TKey, TValue> MergeOverwriteWith<TKey, TValue>(this Dictionary<TKey, TValue>? source, Dictionary<TKey, TValue>? other)
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
