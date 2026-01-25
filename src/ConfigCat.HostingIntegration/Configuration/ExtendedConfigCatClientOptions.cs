using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;

namespace ConfigCat.HostingIntegration.Configuration;

public sealed class ExtendedConfigCatClientOptions : ConfigCatClientOptions
{
    [DisallowNull]
    public string? SdkKey { get; set; }

    // NOTE: ConfigCatClientOptions is not configuration binding-friendly (especially problematic in the case of
    // source generated configuration binding), but we can work this around using a wrapper class.
    internal sealed class BindingWrapper(ExtendedConfigCatClientOptions options)
    {
        public BindingWrapper()
            : this(new ExtendedConfigCatClientOptions()) { }

        public ExtendedConfigCatClientOptions GetOptions() => options;

        [DisallowNull]
        public string? SdkKey
        {
            get => options.SdkKey!;
            set => options.SdkKey = value;
        }

        public PollingOptions? Polling
        {
            get => null; // getter is necessary for source generator, but no need to implement it
            set
            {
                if (value is not null)
                {
                    options.PollingMode = value.Mode switch
                    {
                        PollingModeEnum.AutoPoll => PollingModes.AutoPoll(value.PollInterval, value.MaxInitWaitTime),
                        PollingModeEnum.LazyLoad => PollingModes.LazyLoad(value.CacheTimeToLive),
                        PollingModeEnum.ManualPoll => PollingModes.ManualPoll,
                        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
                    };
                }
            }
        }

        public Uri? BaseUrl
        {
            get => options.BaseUrl;
            set => options.BaseUrl = value;
        }

        public Uri? Proxy
        {
            get => (options.Proxy as WebProxy)?.Address;
            set => options.Proxy = new WebProxy(value);
        }

        public DataGovernance DataGovernance
        {
            get => options.DataGovernance;
            set => options.DataGovernance = value;
        }

        public TimeSpan HttpTimeout
        {
            get => options.HttpTimeout;
            set => options.HttpTimeout = value;
        }

        public bool Offline
        {
            get => options.Offline;
            set => options.Offline = value;
        }
    }

    internal enum PollingModeEnum
    {
        AutoPoll = 0,
        LazyLoad = 1,
        ManualPoll = 2,
    }

    internal sealed class PollingOptions
    {
        public PollingModeEnum Mode { get; set; }

        public TimeSpan? MaxInitWaitTime { get; set; }

        public TimeSpan? PollInterval { get; set; }

        public TimeSpan? CacheTimeToLive { get; set; }
    }
}
