using System.Security.Cryptography;
using System.Text;
using ConfigCat.Client.Utils;

namespace System;

internal static class StringExtensions
{
    public static string Hash(this string text)
    {
        byte[] hashedBytes;
        var textBytes = Encoding.UTF8.GetBytes(text);
#if NET5_0_OR_GREATER
        hashedBytes = SHA1.HashData(textBytes);
#else
        using (var hash = SHA1.Create())
        {
            hashedBytes = hash.ComputeHash(textBytes);
        }
#endif

        return hashedBytes.ToHexString();
    }
}
