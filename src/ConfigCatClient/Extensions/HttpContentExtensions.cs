
using System.IO;
using System.Threading;

namespace System.Net.Http
{
    internal static class HttpContentExtensions
    {
#if NET5_0_OR_GREATER
        public static string ReadAsString(this HttpContent content, CancellationToken token)
        {
            using var responseStream = content.ReadAsStream(token);
            using var reader = new StreamReader(responseStream);
            return reader.ReadToEnd();
        }
#endif
    }
}
