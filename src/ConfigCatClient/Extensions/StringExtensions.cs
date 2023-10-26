using System.Security.Cryptography;
using System.Text;

namespace System;

internal static class StringExtensions
{
    public static byte[] Sha1(this string text)
    {
        var textBytes = Encoding.UTF8.GetBytes(text);
#if NET5_0_OR_GREATER
        return SHA1.HashData(textBytes);
#else
        using var hash = SHA1.Create();
        return hash.ComputeHash(textBytes);
#endif
    }

    public static byte[] Sha256(this byte[] bytes)
    {
#if NET5_0_OR_GREATER
        return SHA256.HashData(bytes);
#else
        using var hash = SHA256.Create();
        return hash.ComputeHash(bytes);
#endif
    }

    public static
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        ReadOnlySpan<char>
#else
        string
#endif
        ToParsable(this ReadOnlySpan<char> s)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return s;
#else
        return s.ToString();
#endif
    }
}
