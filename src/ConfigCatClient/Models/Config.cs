using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ConfigCat.Client.Utils;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using System.Runtime.Serialization;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Details of a ConfigCat config.
/// </summary>
public interface IConfig
{
    /// <summary>
    /// The salt that was used to hash sensitive comparison values.
    /// </summary>
    string? Salt { get; }

    /// <summary>
    /// The list of segments.
    /// </summary>
    IReadOnlyList<ISegment> Segments { get; }

    /// <summary>
    /// The dictionary of settings.
    /// </summary>
    IReadOnlyDictionary<string, ISetting> Settings { get; }
}

internal sealed class Config : IConfig
#if !USE_NEWTONSOFT_JSON
    , IJsonOnDeserialized
#endif
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p")]
#endif
    public Preferences? Preferences { get; set; }

    string? IConfig.Salt => Preferences?.Salt;

    private Segment[]? segments;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "s")]
#else
    [JsonPropertyName("s")]
#endif
    [NotNull]
    public Segment[]? Segments
    {
        get => this.segments ?? ArrayUtils.EmptyArray<Segment>();
        set => this.segments = value;
    }

    private IReadOnlyList<ISegment>? segmentsReadOnly;
    IReadOnlyList<ISegment> IConfig.Segments => this.segmentsReadOnly ??= this.segments is { Length: > 0 }
        ? new ReadOnlyCollection<ISegment>(this.segments)
        : ArrayUtils.EmptyArray<ISegment>();

    private Dictionary<string, Setting>? settings;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "f")]
#else
    [JsonPropertyName("f")]
#endif
    [NotNull]
    public Dictionary<string, Setting>? Settings
    {
        get => this.settings ??= new Dictionary<string, Setting>();
        set => this.settings = value;
    }

    private IReadOnlyDictionary<string, ISetting>? settingsReadOnly;
    IReadOnlyDictionary<string, ISetting> IConfig.Settings => this.settingsReadOnly ??= this.settings is { Count: > 0 }
        ? this.settings.ToDictionary(kvp => kvp.Key, kvp => (ISetting)kvp.Value)
        : new Dictionary<string, ISetting>();

#if USE_NEWTONSOFT_JSON
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context) => OnDeserialized();
#endif

    public void OnDeserialized()
    {
        if (this.settings is { Count: > 0 })
        {
            foreach (var setting in this.settings.Values)
            {
                setting.OnConfigDeserialized(this);
            }
        }
    }
}
