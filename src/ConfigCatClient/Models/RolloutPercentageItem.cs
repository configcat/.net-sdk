using System;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using System.Text.Json.Serialization;
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Percentage option.
/// </summary>
public interface IPercentageOption
{
    /// <summary>
    /// The order value for determining the order of evaluation of rules.
    /// </summary>
    short Order { get; }

    /// <summary>
    /// The value associated with the targeting rule.
    /// </summary>
    object Value { get; }

    /// <summary>
    /// A number between 0 and 100 that represents a randomly allocated fraction of the users.
    /// </summary>
    int Percentage { get; }

    /// <summary>
    /// Variation ID.
    /// </summary>
    string? VariationId { get; set; }
}

internal sealed class RolloutPercentageItem : IPercentageOption
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "o")]
#else
    [JsonPropertyName("o")]
#endif
    public short Order { get; set; }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "v")]
#else
    [JsonPropertyName("v")]
#endif
    public JsonValue Value { get; set; } = default!;

    object IPercentageOption.Value => Value.ConvertToObject(Value.DetermineSettingType());

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p")]
#endif
    public int Percentage { get; set; }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "i")]
#else
    [JsonPropertyName("i")]
#endif
    public string? VariationId { get; set; }

    /// <inheritdoc/>>
    public override string ToString()
    {
        var variationIdString = !string.IsNullOrEmpty(VariationId) ? " [" + VariationId + "]" : string.Empty;
        return $"({Order + 1}) {Percentage}% percent of users => {Value}{variationIdString}";
    }
}
