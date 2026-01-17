using System;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client;
using ConfigCat.Client.Configuration;

namespace ConfigCat.HostingIntegration.Configuration;

public sealed class ExtendedConfigCatClientOptions : ConfigCatClientOptions
{
    [DisallowNull]
    public string? SdkKey { get; set; }

    // NOTE: Unfortunately, it seems that there is no way to ignore properties from configuration binding at the moment,
    // so using source generated configuration binding would result in a bunch of warnings.
    // We can work around the issue by defining a wrapper class for configuration binding.
    internal sealed class BindingWrapper(ExtendedConfigCatClientOptions options)
    {
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
