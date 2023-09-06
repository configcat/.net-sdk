using System;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client.Tests.Helpers;

public partial record class ConfigLocation
{
    public sealed record class Cdn : ConfigLocation
    {
        public Cdn(string sdkKey, string? baseUrl = null) => (SdkKey, BaseUrl) = (sdkKey, baseUrl);

        public string SdkKey { get; }
        public string? BaseUrl { get; }

        public override string RealLocation
        {
            get
            {
                var options = new ConfigCatClientOptions { BaseUrl = BaseUrl is not null ? new Uri(BaseUrl) : ConfigCatClientOptions.BaseUrlEu };
                return options.CreateUri(SdkKey).ToString();
            }
        }

        internal override Config FetchConfig()
        {
            var options = new ConfigCatClientOptions()
            {
                PollingMode = PollingModes.ManualPoll,
                Logger = new ConsoleLogger(),
                BaseUrl = BaseUrl is not null ? new Uri(BaseUrl) : ConfigCatClientOptions.BaseUrlEu
            };

            using var configFetcher = new HttpConfigFetcher(
                options.CreateUri(SdkKey),
                ConfigCatClient.GetProductVersion(options.PollingMode),
                options.Logger!.AsWrapper(),
                options.HttpClientHandler,
                options.IsCustomBaseUrl,
                options.HttpTimeout);

            var fetchResult = configFetcher.Fetch(ProjectConfig.Empty);
            return fetchResult.IsSuccess
                ? fetchResult.Config.Config!
                : throw new InvalidOperationException("Could not fetch config from CDN: " + fetchResult.ErrorMessage);
        }
    }
}
