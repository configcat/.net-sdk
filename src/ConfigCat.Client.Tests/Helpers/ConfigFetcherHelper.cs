using System.Net.Http;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigFetcherHelper
{
    public static readonly HttpClientHandler SharedHandler = new();

    public static HttpClientConfigFetcher CreateFetcherWithSharedHandler()
    {
        return new HttpClientConfigFetcher(delegate
        {
            return new HttpClient(SharedHandler, disposeHandler: false);
        });
    }

    public static HttpClientConfigFetcher CreateFetcherWithCustomHandler(HttpMessageHandler handler)
    {
        return new HttpClientConfigFetcher(delegate
        {
            return new HttpClient(handler, disposeHandler: false);
        });
    }
}
