
using System.IO;

namespace System.Net.Http
{
    internal static class HttpContentExtensions
    {
#if NET5_0_OR_GREATER
        public static string ReadAsString(this HttpContent content)
        {
            using var responseStream = content.ReadAsStream();
            using var reader = new StreamReader(responseStream);
            return reader.ReadToEnd();
        }
#endif
    }
}
