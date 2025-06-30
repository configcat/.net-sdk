using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using ConfigCat.Client.Utils;

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

internal sealed class Config : IConfig, IJsonOnDeserialized
{
    public static Config Deserialize(ReadOnlySpan<char> configJson, bool tolerant = false)
    {
        return SerializationHelper.DeserializeConfig(configJson, tolerant)
            ?? throw new ArgumentException("Invalid config JSON content: " + configJson.ToString(), nameof(configJson));
    }

    [JsonPropertyName("p")]
    public Preferences? Preferences { get; set; }

    string? IConfig.Salt => Preferences?.Salt;

    private Segment[]? segments;

    [JsonPropertyName("s")]
    [NotNull]
    public Segment[]? Segments
    {
        get => this.segments ?? Array.Empty<Segment>();
        set => this.segments = value;
    }

    private IReadOnlyList<ISegment>? segmentsReadOnly;
    IReadOnlyList<ISegment> IConfig.Segments => this.segmentsReadOnly ??= this.segments is { Length: > 0 }
        ? new ReadOnlyCollection<ISegment>(this.segments)
        : Array.Empty<ISegment>();

    private Dictionary<string, Setting>? settings;

    [JsonPropertyName("f")]
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
