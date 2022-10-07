using System.Net;
using System.Runtime.CompilerServices;

namespace ConfigCat.Client.Tests
{
    internal class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Setup()
        {
#if NET45
            // TLS 1.2 was not enabled before .NET 4.6 by default (see https://stackoverflow.com/a/58195987/8656352),
            // so we need to do this because CDN servers are configured to require TLS 1.2+ currently.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
        }
    }
}
