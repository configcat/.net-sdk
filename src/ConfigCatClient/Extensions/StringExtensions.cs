using System.Security.Cryptography;
using System.Text;
using ConfigCat.Client.Utils;

namespace System
{
    internal static class StringExtensions
    {
        public static string Hash(this string text)
        {
            using var hash = SHA1.Create();
            var hashedBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(text));

            return hashedBytes.ToHexString();
        }
    }
}
