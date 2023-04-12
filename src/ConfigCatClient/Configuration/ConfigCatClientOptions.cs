using System;
using System.Net.Http;

namespace ConfigCat.Client.Configuration;

/// <summary>
/// Represents the ConfigCat SDK's configuration options.
/// </summary>
public class ConfigCatClientOptions : IProvidesHooks
{
    internal const string ConfigFileName = "config_v5.json";

    internal static readonly Uri BaseUrlGlobal = new("https://cdn-global.configcat.com");

    internal static readonly Uri BaseUrlEu = new("https://cdn-eu.configcat.com");

    private IConfigCatLogger logger = new ConsoleLogger(LogLevel.Warning);

    /// <summary>
    /// Logger instance. If you want to use custom logging instead of the client's default <see cref="ConsoleLogger"/>, you can provide an implementation of <see cref="IConfigCatLogger"/>.
    /// </summary>
    public IConfigCatLogger Logger
    {
        get => this.logger;
        set => this.logger = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// If you want to use custom caching instead of the client's default <see cref="InMemoryConfigCache"/>, you can provide an implementation of <see cref="IConfigCatCache"/>.
    /// </summary>
    public IConfigCatCache ConfigCache { get; set; }

    /// <summary>
    /// Polling mode. Defaults to auto polling.
    /// </summary>
    public PollingMode PollingMode { get; set; } = PollingModes.AutoPoll();

    /// <summary>
    /// HttpClientHandler to provide network credentials and proxy settings.
    /// </summary>
    public HttpClientHandler HttpClientHandler { get; set; }

    /// <summary>
    /// You can set a BaseUrl if you want to use a proxy server between your application and ConfigCat.
    /// </summary>
    public Uri BaseUrl { get; set; } = BaseUrlGlobal;

    internal bool IsCustomBaseUrl => BaseUrl != BaseUrlGlobal && BaseUrl != BaseUrlEu;

    /// <summary>
    /// Default: Global. Set this parameter to be in sync with the Data Governance preference on the Dashboard:
    /// https://app.configcat.com/organization/data-governance (Only Organization Admins have access)
    /// </summary>
    public DataGovernance DataGovernance { get; set; } = DataGovernance.Global;

    /// <summary>
    /// Timeout for underlying http calls. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Feature flag and setting overrides.
    /// </summary>
    public FlagOverrides FlagOverrides { get; set; }

    /// <summary>
    /// The default user, used as fallback when there's no user parameter is passed to the <see cref="ConfigCatClient.GetValue{T}(string, T, User)"/>, <see cref="ConfigCatClient.GetAllValues(User)"/>, etc. methods.
    /// </summary>
    public User DefaultUser { get; set; }

    /// <summary>
    /// Indicates whether the client should be initialized to offline mode or not. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Offline { get; set; }

    internal Hooks Hooks { get; } = new Hooks();

    internal void Validate()
    {
        if (Logger is null)
        {
            throw new ArgumentNullException(nameof(Logger));
        }

        PollingMode.Validate();
    }

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
    public event EventHandler ClientReady
    {
        add { Hooks.ClientReady += value; }
        remove { Hooks.ClientReady -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
    {
        add { Hooks.FlagEvaluated += value; }
        remove { Hooks.FlagEvaluated -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigChangedEventArgs> ConfigChanged
    {
        add { Hooks.ConfigChanged += value; }
        remove { Hooks.ConfigChanged -= value; }
    }

    /// <inheritdoc/>
    public event EventHandler<ConfigCatClientErrorEventArgs> Error
    {
        add { Hooks.Error += value; }
        remove { Hooks.Error -= value; }
    }
}
