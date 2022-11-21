namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> MergeOverwriteWith<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other)
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
}
