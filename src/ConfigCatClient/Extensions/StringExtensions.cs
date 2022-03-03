using System.Security.Cryptography;
using System.Text;

namespace System
{
    internal static class StringExtensions
    {
        public static string Hash(this string text)
        {
            using var hash = SHA1.Create();
            var hashedBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(text));

            var result = new StringBuilder();

            foreach (var t in hashedBytes)
            {
                result.Append(t.ToString("x2"));
            }

            return result.ToString();
        }
    }
}
