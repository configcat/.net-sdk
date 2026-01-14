using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace System;

internal static class StringExtensions
{
    public static byte[] Sha1(this string text)
    {
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
            return new ArraySegment<byte>(bytes, 0, byteCount).Sha1();
        }
        finally { ArrayPool<byte>.Shared.Return(bytes); }
    }

    public static byte[] Sha1(this ArraySegment<byte> bytes)
    {
#if NET5_0_OR_GREATER
        return SHA1.HashData(bytes.AsSpan());
#else
        using var hash = SHA1.Create();
        return hash.ComputeHash(bytes.Array, bytes.Offset, bytes.Count);
#endif
    }

    public static byte[] Sha256(this ArraySegment<byte> bytes)
    {
#if NET5_0_OR_GREATER
        return SHA256.HashData(bytes.AsSpan());
#else
        using var hash = SHA256.Create();
        return hash.ComputeHash(bytes.Array, bytes.Offset, bytes.Count);
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
