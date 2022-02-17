using System;

namespace ConfigCat.Client.Utils
{
    internal static class ArrayUtils
    {
        public static T[] EmptyArray<T>() =>
#if NET45
            ArrayHelper<T>.Empty;
#else
            Array.Empty<T>();
#endif

#if NET45
        private static class ArrayHelper<T>
        {
            public static readonly T[] Empty = new T[0];
        }
#endif
    }
}
