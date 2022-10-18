#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using System.Text.Json.Serialization;
using System.Text.Json;
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client.Evaluation
{
    /// <summary>
    /// Percentage-based targeting rule.
    /// </summary>
    public record class RolloutPercentageItem
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
        /// The value associated with the targeting rule.
        /// </summary>
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "v")]
#else
        [JsonPropertyName("v")]
#endif
        public JsonValue Value { get; set; }

        /// <summary>
        /// A number between 0 and 100 that represents a randomly allocated fraction of the users.
        /// </summary>
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "p")]
#else
        [JsonPropertyName("p")]
#endif
        public int Percentage { get; set; }

        /// <summary>
        /// Variation ID.
        /// </summary>
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "i")]
#else
        [JsonPropertyName("i")]
#endif
        public string VariationId { get; set; }

        /// <inheritdoc/>>
        public override string ToString()
        {
            var variationIdString = !string.IsNullOrEmpty(VariationId) ? " [" + VariationId + "]" : string.Empty;
            return $"({Order + 1}) {Percentage}% percent of users => {Value}{variationIdString}";
        }
    }
}