using System.IO;
using System.Threading;

namespace System.Net.Http;

#if NET5_0_OR_GREATER
internal static class HttpContentExtensions
{
    public static string ReadAsString(this HttpContent content, CancellationToken token)
    {
        using var responseStream = content.ReadAsStream(token);
        using var reader = new StreamReader(responseStream);
        return reader.ReadToEnd();
    }
}
#endif
