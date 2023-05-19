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
/// Targeting rule.
/// </summary>
public interface ITargetingRule
{
    /// <summary>
    /// The order value for determining the order of evaluation of rules.
    /// </summary>
    short Order { get; }

    /// <summary>
    /// The attribute that the targeting rule is based on. Could be "User ID", "Email", "Country" or any custom attribute.
    /// </summary>
    string ComparisonAttribute { get; }

    /// <summary>
    /// The comparison operator. Defines the connection between the attribute and the value.
    /// </summary>
    Comparator Comparator { get; }

    /// <summary>
    /// The value that the attribute is compared to. Could be a string, a number, a semantic version or a comma-separated list, depending on the comparator.
    /// </summary>
    string ComparisonValue { get; }

    /// <summary>
    /// The value associated with the targeting rule.
    /// </summary>
    object Value { get; }

    /// <summary>
    /// Variation ID.
    /// </summary>
    string? VariationId { get; }
}

internal sealed class RolloutRule : ITargetingRule
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "o")]
#else
    [JsonPropertyName("o")]
#endif
    public short Order { get; set; }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "a")]
#else
    [JsonPropertyName("a")]
#endif
    public string ComparisonAttribute { get; set; } = null!;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "t")]
#else
    [JsonPropertyName("t")]
#endif
    public Comparator Comparator { get; set; }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "c")]
#else
    [JsonPropertyName("c")]
#endif
    public string ComparisonValue { get; set; } = null!;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "v")]
#else
    [JsonPropertyName("v")]
#endif
    public JsonValue Value { get; set; } = default!;

    object ITargetingRule.Value => Value.ConvertToObject(Value.DetermineSettingType());

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "i")]
#else
    [JsonPropertyName("i")]
#endif
    public string? VariationId { get; set; }

    internal static string FormatComparator(Comparator comparator)
    {
        return comparator switch
        {
            Comparator.In => "IS ONE OF",
            Comparator.SemVerIn => "IS ONE OF",
            Comparator.NotIn => "IS NOT ONE OF",
            Comparator.SemVerNotIn => "IS NOT ONE OF",
            Comparator.Contains => "CONTAINS",
            Comparator.NotContains => "DOES NOT CONTAIN",
            Comparator.SemVerLessThan => "<",
            Comparator.NumberLessThan => "<",
            Comparator.SemVerLessThanEqual => "<=",
            Comparator.NumberLessThanEqual => "<=",
            Comparator.SemVerGreaterThan => ">",
            Comparator.NumberGreaterThan => ">",
            Comparator.SemVerGreaterThanEqual => ">=",
            Comparator.NumberGreaterThanEqual => ">=",
            Comparator.NumberEqual => "=",
            Comparator.NumberNotEqual => "!=",
            Comparator.SensitiveOneOf => "IS ONE OF (hashed)",
            Comparator.SensitiveNotOneOf => "IS NOT ONE OF (hashed)",
            _ => comparator.ToString()
        };
    }

    /// <inheritdoc/>>
    public override string ToString()
    {
        var variationIdString = !string.IsNullOrEmpty(VariationId) ? " [" + VariationId + "]" : string.Empty;
        return $"({Order + 1}) {(Order > 0 ? "ELSE " : string.Empty)}IF user's {ComparisonAttribute} {FormatComparator(Comparator)} '{ComparisonValue}' => {Value}{variationIdString}";
    }
}
