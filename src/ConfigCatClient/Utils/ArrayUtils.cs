using System;

namespace ConfigCat.Client.Utils;

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
#pragma warning disable IDE0055 // Fix formatting
            var i = 0;
            foreach (var b in bytes)
            {
                chars[i++] = hexDigits[b >> 4];
                chars[i++] = hexDigits[b & 0xF];
            }
#pragma warning restore IDE0055 // Fix formatting
#if (NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
        });
#else
        return new string(chars);
#endif
    }

    public static bool Equals(this byte[] bytes, ReadOnlySpan<char> hexString)
    {
        if (bytes.Length * 2 != hexString.Length)
        {
            return false;
        }

        for (int i = 0, j = 0; i < bytes.Length; i++)
        {
            int hi, lo;
            if ((hi = GetDigitValue(hexString[j++])) < 0
                || (lo = GetDigitValue(hexString[j++])) < 0)
            {
                throw new FormatException();
            }

            var decodedByte = (byte)(hi << 4 | lo);
            if (decodedByte != bytes[i])
            {
                return false;
            }
        }

        return true;

        static int GetDigitValue(char digit) => digit switch
        {
            >= '0' and <= '9' => digit - 0x30,
            >= 'a' and <= 'f' => digit - 0x57,
            _ => -1,
        };
    }
}
