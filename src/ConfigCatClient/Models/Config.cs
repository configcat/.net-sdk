using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a ConfigCat config's data model used for feature flag evaluation.
/// </summary>
public sealed class Config : IJsonOnDeserialized
{
    /// <summary>
    /// Deserializes the specified config JSON to a <see cref="Config"/> model that can be used for feature flag evaluation.
    /// </summary>
    /// <remarks>
    /// Does superficial model validation only, meaning that the method makes sure that the specified config JSON
    /// matches the type definition of the <see cref="Config"/> model, but doesn't check for semantic issues. E.g. doesn't validate
    /// whether referenced segments and feature flags actually exist. (Such issues are checked during feature flag evaluation.)
    /// </remarks>
    public static Config Deserialize(ReadOnlySpan<char> configJson)
    {
        return Deserialize(configJson, tolerant: true);
    }

    internal static Config Deserialize(ReadOnlySpan<char> configJson, bool tolerant)
    {
        return SerializationHelper.DeserializeConfig(configJson, tolerant)
            ?? throw new ArgumentException("Invalid config JSON content: " + configJson.ToString(), nameof(configJson));
    }

    [JsonConstructor]
    internal Config() { }

    [JsonInclude, JsonPropertyName("p")]
    internal Preferences? preferences;

    /// <summary>
    /// The salt that was used to hash sensitive comparison values.
    /// </summary>
    [JsonIgnore]
    public string? Salt => this.preferences?.Salt;

    [JsonInclude, JsonPropertyName("s")]
    internal Segment[]? segments;

    internal Segment[] SegmentsOrEmpty => this.segments ?? Array.Empty<Segment>();

    private IReadOnlyList<Segment>? segmentsReadOnly;

    /// <summary>
    /// The list of segments.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Segment> Segments => this.segmentsReadOnly ??= this.segments is { Length: > 0 }
        ? new ReadOnlyCollection<Segment>(this.segments)
        : Array.Empty<Segment>();

    [JsonInclude, JsonPropertyName("f")]
    internal Dictionary<string, Setting>? settings;

    internal Dictionary<string, Setting> SettingsOrEmpty => this.settings ??= new Dictionary<string, Setting>();

    private IReadOnlyDictionary<string, Setting>? settingsReadOnly;

    /// <summary>
    /// The dictionary of settings.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, Setting> Settings => this.settingsReadOnly ??= this.settings is { Count: > 0 }
        ? this.settings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        : new Dictionary<string, Setting>();

    void IJsonOnDeserialized.OnDeserialized()
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
