using System;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client.Tests.Helpers;

public partial record class ConfigLocation
{
    public sealed record class Cdn : ConfigLocation
    {
        public Cdn(string sdkKey, string? baseUrl = null) => (SdkKey, BaseUrl) = (sdkKey, baseUrl);

        public string SdkKey { get; }
        public string? BaseUrl { get; }

        public override string GetRealLocation()
        {
            var options = new ConfigCatClientOptions();
            ConfigureBaseUrl(options);
            return ConfigCatClientOptions.GetConfigUri(options.GetBaseUri(), SdkKey).ToString();
        }

        internal override Config FetchConfig()
        {
            var options = new ConfigCatClientOptions()
            {
                PollingMode = PollingModes.ManualPoll,
                Logger = new ConsoleLogger(),
            };
            ConfigureBaseUrl(options);

            using var configFetcher = new DefaultConfigFetcher(
                SdkKey,
                options.GetBaseUri(),
                ConfigCatClient.GetProductVersion(options.PollingMode),
                options.Logger!.AsWrapper(),
                new HttpClientConfigFetcher(options.HttpClientHandler),
                options.IsCustomBaseUrl,
                options.HttpTimeout);

            var fetchResult = Task.Run(() => configFetcher.FetchAsync(ProjectConfig.Empty)).GetAwaiter().GetResult();
            return fetchResult.IsSuccess
                ? fetchResult.Config.Config!
                : throw new InvalidOperationException("Could not fetch config from CDN: " + fetchResult.ErrorMessage);
        }

        internal void ConfigureBaseUrl(ConfigCatClientOptions options)
        {
            options.BaseUrl = BaseUrl is not null ? new Uri(BaseUrl) : ConfigCatClientOptions.BaseUrlEu;
        }
    }
}
