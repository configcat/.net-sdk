namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> MergeOverwriteWith<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var item in source)
                result[item.Key] = item.Value;

            foreach (var item in other)
                result[item.Key] = item.Value;

            return result;
        }
    }
}
