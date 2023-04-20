namespace System.Collections.Generic;

internal static class DictionaryExtensions
{
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

    public static IReadOnlyCollection<TKey> ReadOnlyKeys<TKey, TValue>(this Dictionary<TKey, TValue> source)
        where TKey : notnull
    {
#if !NET45
        return source.Keys;
#else
        return new ReadOnlyCollectionAdapter<TKey>(source.Keys);
#endif
    }

#if NET45
    private sealed class ReadOnlyCollectionAdapter<T> : IReadOnlyCollection<T>
    {
        private readonly ICollection<T> collection;

        public ReadOnlyCollectionAdapter(ICollection<T> collection)
        {
            this.collection = collection;
        }

        public int Count => this.collection.Count;

        public IEnumerator<T> GetEnumerator() => this.collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
#endif
}
