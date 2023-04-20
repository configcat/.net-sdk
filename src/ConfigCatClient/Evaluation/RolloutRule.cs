#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using System.Text.Json.Serialization;
using System.Text.Json;
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client.Evaluation;

/// <summary>
/// Comparison-based targeting rule.
/// </summary>
public record class RolloutRule
{
    /// <summary>
    /// The order value for determining the order of evaluation of rules.
    /// </summary>
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "o")]
#else
    [JsonPropertyName("o")]
#endif
    public short Order { get; set; }

    /// <summary>
    /// The attribute that the targeting rule is based on. Could be "User ID", "Email", "Country" or any custom attribute.
    /// </summary>
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "a")]
#else
    [JsonPropertyName("a")]
#endif
    public string ComparisonAttribute { get; set; } = null!;

    /// <summary>
    /// The comparison operator. Defines the connection between the attribute and the value.
    /// </summary>
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "t")]
#else
    [JsonPropertyName("t")]
#endif
    public Comparator Comparator { get; set; }

    /// <summary>
    /// The value that the attribute is compared to. Could be a string, a number, a semantic version or a comma-separated list, depending on the comparator.
    /// </summary>
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "c")]
#else
    [JsonPropertyName("c")]
#endif
    public string ComparisonValue { get; set; } = null!;

    /// <summary>
    /// The value associated with the targeting rule.
    /// </summary>
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "v")]
#else
    [JsonPropertyName("v")]
#endif
    public JsonValue Value { get; set; } = default!;

    /// <summary>
    /// Variation ID.
    /// </summary>
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

/// <summary>
/// Operator of comparison-based targeting rules.
/// </summary>
public enum Comparator : byte
{
    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of strings contain the comparison attribute?
    /// </summary>
    In = 0,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of strings not contain the comparison attribute?
    /// </summary>
    NotIn = 1,

    /// <summary>
    /// Does the comparison value contain the comparison attribute as a substring?
    /// </summary>
    Contains = 2,

    /// <summary>
    /// Does the comparison value not contain the comparison attribute as a substring?
    /// </summary>
    NotContains = 3,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of semantic versions contain the comparison attribute?
    /// </summary>
    SemVerIn = 4,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of semantic versions not contain the comparison attribute?
    /// </summary>
    SemVerNotIn = 5,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version less than the comparison attribute?
    /// </summary>
    SemVerLessThan = 6,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version less than or equal to the comparison attribute?
    /// </summary>
    SemVerLessThanEqual = 7,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version greater than the comparison attribute?
    /// </summary>
    SemVerGreaterThan = 8,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version greater than or equal to the comparison attribute?
    /// </summary>
    SemVerGreaterThanEqual = 9,

    /// <summary>
    /// Is the comparison value interpreted as a number equal to the comparison attribute?
    /// </summary>
    NumberEqual = 10,

    /// <summary>
    /// Is the comparison value interpreted as a number not equal to the comparison attribute?
    /// </summary>
    NumberNotEqual = 11,

    /// <summary>
    /// Is the comparison value interpreted as a number less than the comparison attribute?
    /// </summary>
    NumberLessThan = 12,

    /// <summary>
    /// Is the comparison value interpreted as a number less than or equal to the comparison attribute?
    /// </summary>
    NumberLessThanEqual = 13,

    /// <summary>
    /// Is the comparison value interpreted as a number greater than the comparison attribute?
    /// </summary>
    NumberGreaterThan = 14,

    /// <summary>
    /// Is the comparison value interpreted as a number greater than or equal to the comparison attribute?
    /// </summary>
    NumberGreaterThanEqual = 15,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of hashes of strings contain the hash of the comparison attribute?
    /// </summary>
    SensitiveOneOf = 16,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of hashes of strings not contain the hash of the comparison attribute?
    /// </summary>
    SensitiveNotOneOf = 17
}
