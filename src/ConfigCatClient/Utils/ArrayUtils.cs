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

        public static string ToHexString(this byte[] bytes)
        {
            const string hexDigits = "0123456789abcdef";

#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
            var chars = new char[bytes.Length * 2];
#else
            return string.Create(bytes.Length * 2, bytes, static (chars, bytes) =>
            {
#endif
                var i = 0;
                foreach (var b in bytes)
                {
                    chars[i++] = hexDigits[b >> 4];
                    chars[i++] = hexDigits[b & 0xF];
                }
#if (NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
            });
#else
            return new string(chars);
#endif
        }
    }
}
