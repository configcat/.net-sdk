using System;
using System.Net.Http;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigFetcherHelper
{
    private static readonly HttpClientHandler SharedHandler = new();

    public static HttpClientConfigFetcher CreateFetcherWithSharedHandler(TimeSpan? timeout = null)
    {
        return CreateFetcherWithCustomHandler(SharedHandler, timeout);
    }

    public static HttpClientConfigFetcher CreateFetcherWithCustomHandler(HttpMessageHandler handler, TimeSpan? timeout = null)
    {
        return new HttpClientConfigFetcher(delegate
        {
            var client = new HttpClient(handler, disposeHandler: false);
            if (timeout is not null)
            {
                client.Timeout = timeout.Value;
            }
            return client;
        });
    }
}
