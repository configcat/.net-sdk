using System.Security.Cryptography;
using System.Text;

namespace ConfigCat.Client.Security
{
    internal class HashUtils
    {
        public static string HashString(string s)
        {
            using (var hash = SHA1.Create())
            {
                var hashedBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(s));

                var result = new StringBuilder();

                foreach (var t in hashedBytes)
                {
                    result.Append(t.ToString("x2"));
                }

                return result.ToString();
            }
        }
    }
}
