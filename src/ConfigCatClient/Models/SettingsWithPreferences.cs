using System.Collections.Generic;
using System.Linq;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// ConfigCat config.
/// </summary>
public interface IConfig
{
    /// <summary>
    /// The dictionary of settings.
    /// </summary>
    IReadOnlyDictionary<string, ISetting> Settings { get; }
}

internal sealed class SettingsWithPreferences : IConfig
{
    private Dictionary<string, Setting>? settings;
    private IReadOnlyDictionary<string, ISetting>? settingsReadOnly;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "f")]
#else
    [JsonPropertyName("f"), JsonInclude]
#endif
    public Dictionary<string, Setting> Settings
    {
        get => this.settings ??= new Dictionary<string, Setting>();
        private set => this.settings = value;
    }

    IReadOnlyDictionary<string, ISetting> IConfig.Settings => this.settingsReadOnly ??= Settings.ToDictionary(kvp => kvp.Key, kvp => (ISetting)kvp.Value);

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p")]
#endif
    public Preferences? Preferences { get; set; }
}
