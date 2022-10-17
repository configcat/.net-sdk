using System.Collections.Generic;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json.Serialization;
using System.Text.Json;
#endif

namespace ConfigCat.Client.Evaluation
{
    internal class SettingsWithPreferences
    {
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "f")]
#else
        [JsonPropertyName("f")]
#endif
        public Dictionary<string, Setting> Settings { get; set; }

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "p")]
#else
        [JsonPropertyName("p")]
#endif
        public Preferences Preferences { get; set; }
    }

    internal class Preferences
    {
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "u")]
#else
        [JsonPropertyName("u")]
#endif
        public string Url { get; set; }

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "r")]
#else
        [JsonPropertyName("r")]
#endif
        public RedirectMode RedirectMode { get; set; }
    }

    internal class Setting
    {
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "v")]
        public JValue Value { get; set; }
#else
        [JsonPropertyName("v")]
        public JsonElement Value { get; set; }
#endif

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "t")]
#else
        [JsonPropertyName("t")]
#endif
        public SettingType SettingType { get; set; }

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "p")]
#else
        [JsonPropertyName("p")]
#endif
        public List<RolloutPercentageItem> RolloutPercentageItems { get; set; } = new List<RolloutPercentageItem>();

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "r")]
#else
        [JsonPropertyName("r")]
#endif
        public List<RolloutRule> RolloutRules { get; set; } = new List<RolloutRule>();

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "i")]
#else
        [JsonPropertyName("i")]
#endif
        public string VariationId { get; set; }
    }

    internal class RolloutPercentageItem
    {
#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "o")]
#else
        [JsonPropertyName("o")]
#endif
        public short Order { get; set; }

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "v")]
        public JValue Value { get; set; }
#else
        [JsonPropertyName("v")]
        public JsonElement Value { get; set; }
#endif

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
        public string VariationId { get; set; }
    }

    internal class RolloutRule
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
        public string ComparisonAttribute { get; set; }

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
        public string ComparisonValue { get; set; }

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "v")]
        public JValue Value { get; set; }
#else
        [JsonPropertyName("v")]
        public JsonElement Value { get; set; }
#endif

#if USE_NEWTONSOFT_JSON
        [JsonProperty(PropertyName = "i")]
#else
        [JsonPropertyName("i")]
#endif
        public string VariationId { get; set; }
    }

    internal enum SettingType : byte
    {
        Boolean = 0,
        String = 1,
        Int = 2,
        Double = 3
    }

    internal enum Comparator : byte
    {
        In = 0,
        NotIn = 1,
        Contains = 2,
        NotContains = 3,
        SemVerIn = 4,
        SemVerNotIn = 5,
        SemVerLessThan = 6,
        SemVerLessThanEqual = 7,
        SemVerGreaterThan = 8,
        SemVerGreaterThanEqual = 9,
        NumberEqual = 10,
        NumberNotEqual = 11,
        NumberLessThan = 12,
        NumberLessThanEqual = 13,
        NumberGreaterThan = 14,
        NumberGreaterThanEqual = 15,
        SensitiveOneOf = 16,
        SensitiveNotOneOf = 17
    }

    internal enum RedirectMode : byte
    {
        No = 0,
        Should = 1,
        Force = 2
    }
}