#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// A model object which contains a setting value along with related data.
/// </summary>
public interface ISettingValueContainer
{
    /// <summary>
    /// Setting value. Can be a value of the following types: <see cref="bool"/>, <see cref="string"/>, <see cref="int"/> or <see cref="double"/>.
    /// </summary>
    object Value { get; }

    /// <summary>
    /// Variation ID.
    /// </summary>
    string? VariationId { get; }
}

internal abstract class SettingValueContainer : ISettingValueContainer
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "v")]
#else
    [JsonPropertyName("v")]
#endif
    public SettingValue Value { get; set; }

    object ISettingValueContainer.Value => Value.GetValue()!;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "i")]
#else
    [JsonPropertyName("i")]
#endif
    public string? VariationId { get; set; }
}

// NOTE: This sealed class is for fast type checking in TargetingRule.SimpleValue
// (see also https://stackoverflow.com/a/70065177/8656352).
internal sealed class SimpleSettingValue : SettingValueContainer
{
}
