using System.Text.Json.Serialization;

namespace ConfigCat.Client;

/// <summary>
/// A model object which contains a setting value along with related data.
/// </summary>
public abstract class SettingValueContainer
{
    private protected SettingValueContainer() { }

    [JsonInclude, JsonPropertyName("v")]
    internal SettingValue value;

    /// <summary>
    /// Setting value.
    /// Can be a value of the following types: <see cref="bool"/>, <see cref="string"/>, <see cref="int"/> or <see cref="double"/>.
    /// </summary>
    [JsonIgnore]
    public object Value => this.value.GetValue()!;

    [JsonInclude, JsonPropertyName("i")]
    internal string? variationId;

    /// <summary>
    /// Variation ID.
    /// </summary>
    [JsonIgnore]
    public string? VariationId => this.variationId;
}

// NOTE: This sealed class is for fast type checking in TargetingRule.SimpleValueOrNull
// (see also https://stackoverflow.com/a/70065177/8656352).
internal sealed class SimpleSettingValue : SettingValueContainer
{
}
