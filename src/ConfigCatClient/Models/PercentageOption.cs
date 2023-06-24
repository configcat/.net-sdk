#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Percentage option.
/// </summary>
public interface IPercentageOption : ISettingValueContainer
{
    /// <summary>
    /// A number between 0 and 100 that represents a randomly allocated fraction of the users.
    /// </summary>
    int Percentage { get; }
}

internal sealed class PercentageOption : SettingValueContainer, IPercentageOption
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p")]
#endif
    public int Percentage { get; set; }

    // TODO
    ///// <inheritdoc/>
    //public override string ToString()
    //{
    //    var variationIdString = !string.IsNullOrEmpty(VariationId) ? " [" + VariationId + "]" : string.Empty;
    //    return $"({Order + 1}) {Percentage}% percent of users => {Value}{variationIdString}";
    //}
}
