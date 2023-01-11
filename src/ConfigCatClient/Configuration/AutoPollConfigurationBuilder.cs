using System;
using System.Net.Http;

namespace ConfigCat.Client;

/// <summary>
/// Configuration builder for AutoPoll mode
/// </summary>
[Obsolete("This class is obsolete and will be removed from the public API in a future major version. To obtain a ConfigCatClient instance with auto polling for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.AutoPoll(); })' format.")]
public class AutoPollConfigurationBuilder : ConfigurationBuilderBase<AutoPollConfiguration>
{
    internal AutoPollConfigurationBuilder(ConfigCatClientBuilder clientBuilder) : base(clientBuilder) { }

    /// <summary>
    /// Configuration refresh period
    /// </summary>
    [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.AutoPoll(pollIntervalSeconds: TimeSpan.FromSeconds(60)); })' format.")]
    public AutoPollConfigurationBuilder WithPollIntervalSeconds(uint pollIntervalSeconds)
    {
        this.Configuration.PollIntervalSeconds = pollIntervalSeconds;

        return this;
    }

    /// <summary>
    /// Maximum waiting time between initialization and the first config acquisition in seconds. (Default value is 5.)
    /// </summary>
    [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.AutoPoll(maxInitWaitTimeSeconds: TimeSpan.FromSeconds(5)); })' format.")]
    public AutoPollConfigurationBuilder WithMaxInitWaitTimeSeconds(uint maxInitWaitTimeSeconds)
    {
        this.Configuration.MaxInitWaitTimeSeconds = maxInitWaitTimeSeconds;

        return this;
    }

    /// <summary>
    /// If you want to use custom caching instead of the client's default InMemoryConfigCache, You can provide an implementation of IConfigCache.
    /// </summary>
    [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.ConfigCache = /* your cache */; })' format.")]
    public AutoPollConfigurationBuilder WithConfigCache(IConfigCache configCache)
    {
        this.Configuration.ConfigCache = configCache;

        return this;
    }

    /// <summary>
    /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat
    /// </summary>
    [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.BaseUrl = new Uri(/* base url */); })' format.")]
    public AutoPollConfigurationBuilder WithBaseUrl(Uri baseUrl)
    {
        this.Configuration.BaseUrl = baseUrl;

        return this;
    }

    /// <summary>
    /// HttpClientHandler to provide network credentials and proxy settings
    /// </summary>
    [Obsolete("Please use the 'ConfigCatClient.Get(sdkKey, options => { options.HttpClientHandler = /* http client handler */; })' format.")]
    public AutoPollConfigurationBuilder WithHttpClientHandler(HttpClientHandler httpClientHandler)
    {
        this.Configuration.HttpClientHandler = httpClientHandler;

        return this;
    }

    /// <summary>
    /// Create a <see cref="IConfigCatClient"/> instance
    /// </summary>
    /// <returns></returns>
    [Obsolete("To obtain a ConfigCatClient instance with auto polling for a specific SDK Key, please use the 'ConfigCatClient.Get(sdkKey, options => { options.PollingMode = PollingModes.AutoPoll(); })' format.")]
    public IConfigCatClient Create()
    {
        return new ConfigCatClient(this.Configuration);
    }
}
