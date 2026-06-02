using System;
using System.Buffers;

namespace ConfigCat.Client.Utils;

internal static class ArrayUtils
{
    public static string ToHexString(this byte[] bytes)
    {
        const string hexDigits = "0123456789abcdef";
        var length = bytes.Length * 2;

#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
        var charArray = ArrayPool<char>.Shared.Rent(length);
        try
        {
            var chars = charArray.AsSpan(0, length);
#else
        return string.Create(length, bytes, static (chars, bytes) =>
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
            return new string(charArray, 0, length);
        }
        finally { ArrayPool<char>.Shared.Return(charArray); }
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
                return false;
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
