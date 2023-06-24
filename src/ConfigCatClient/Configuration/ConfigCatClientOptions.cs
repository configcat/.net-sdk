using System;
using System.Net.Http;
using ConfigCat.Client.Cache;

namespace ConfigCat.Client.Configuration;

/// <summary>
/// Options used to configure the ConfigCat SDK.
/// </summary>
public class ConfigCatClientOptions : IProvidesHooks
{
    internal const string ConfigFileName = "config_v6.json";

    internal static readonly Uri BaseUrlGlobal = new("https://cdn-global.configcat.com");

    internal static readonly Uri BaseUrlEu = new("https://cdn-eu.configcat.com");

    /// <summary>
    /// The logger implementation to use for performing logging.
    /// If not set, <see cref="ConsoleLogger"/> with <see cref="LogLevel.Warning"/> will be used by default.<br/>
    /// If you want to use custom logging instead, you can provide an implementation of <see cref="IConfigCatLogger"/>.
    /// </summary>
    public IConfigCatLogger? Logger { get; set; }

    internal static IConfigCatLogger CreateDefaultLogger() => new ConsoleLogger(LogLevel.Warning);

    /// <summary>
    /// The cache implementation to use for storing and retrieving downloaded config data.
    /// If not set, <see cref="InMemoryConfigCache"/> will be used by default.<br/>
    /// If you want to use custom caching instead, you can provide an implementation of <see cref="IConfigCatCache"/>.
    /// </summary>
    public IConfigCatCache? ConfigCache { get; set; }

    internal static ConfigCache CreateDefaultConfigCache() => new InMemoryConfigCache();

    /// <summary>
    /// The polling mode to use.
    /// If not set, <see cref="PollingModes.AutoPoll"/> will be used by default.
    /// </summary>
    public PollingMode? PollingMode { get; set; }

    internal static PollingMode CreateDefaultPollingMode() => PollingModes.AutoPoll();

    /// <summary>
    /// An optional <see cref="System.Net.Http.HttpClientHandler"/> for providing network credentials and proxy settings.
    /// </summary>
    public HttpClientHandler? HttpClientHandler { get; set; }

    private Uri baseUrl = BaseUrlGlobal;

    /// <summary>
    /// The base URL of the remote server providing the latest version of the config.
    /// Defaults to the URL of the ConfigCat CDN.<br/>
    /// If you want to use a proxy server between your application and ConfigCat, you need to set this property to the proxy URL.
    /// </summary>
    public Uri BaseUrl
    {
        get => this.baseUrl;
        set => this.baseUrl = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal bool IsCustomBaseUrl => BaseUrl != BaseUrlGlobal && BaseUrl != BaseUrlEu;

    /// <summary>
    /// Set this property to be in sync with the Data Governance preference on the Dashboard:
    /// https://app.configcat.com/organization/data-governance (only Organization Admins have access).
    /// Defaults to <see cref="DataGovernance.Global"/>.
    /// </summary>
    public DataGovernance DataGovernance { get; set; } = DataGovernance.Global;

    /// <summary>
    /// Timeout for underlying HTTP calls. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The flag override to use. If not set, no flag override will be used.
    /// </summary>
    public FlagOverrides? FlagOverrides { get; set; }

    /// <summary>
    /// The default user, used as fallback when there's no user parameter is passed to the setting evaluation methods like <see cref="IConfigCatClient.GetValue"/>, <see cref="IConfigCatClient.GetValueDetails"/>, etc.
    /// </summary>
    public User? DefaultUser { get; set; }

    /// <summary>
    /// Indicates whether the client should be initialized to offline mode or not. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Offline { get; set; }

    internal Hooks Hooks { get; } = new Hooks();

    internal Uri CreateUri(string sdkKey)
    {
        var baseUri = BaseUrl;

        if (!IsCustomBaseUrl)
        {
            baseUri = DataGovernance switch
            {
                DataGovernance.EuOnly => BaseUrlEu,
                _ => BaseUrlGlobal,
            };
        }

        return new Uri(baseUri, "configuration-files/" + sdkKey + "/" + ConfigFileName);
    }

    /// <inheritdoc/>
    public event EventHandler? ClientReady
    {
        add { Hooks.ClientReady += value; }
        remove { Hooks.ClientReady -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<FlagEvaluatedEventArgs>? FlagEvaluated
    {
        add { Hooks.FlagEvaluated += value; }
        remove { Hooks.FlagEvaluated -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged
    {
        add { Hooks.ConfigChanged += value; }
        remove { Hooks.ConfigChanged -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigCatClientErrorEventArgs>? Error
    {
        add { Hooks.Error += value; }
        remove { Hooks.Error -= value; }
    }
}
