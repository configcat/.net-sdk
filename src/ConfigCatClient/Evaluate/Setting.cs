using System.Collections.Generic;
using Newtonsoft.Json;

namespace ConfigCat.Client.Evaluate
{
    internal class SettingsWithPreferences
    {
        [JsonProperty(PropertyName = "f")]
        public Dictionary<string, Setting> Settings { get; set; }

        [JsonProperty(PropertyName = "p")]
        public Preferences Preferences { get; set; }
    }

    internal class Preferences
    {
        [JsonProperty(PropertyName = "u")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "r")]
        public RedirectMode RedirectMode { get; set; }
    }

    internal class Setting
    {
        [JsonProperty(PropertyName = "v")]
        public string RawValue { get; set; }

        [JsonProperty(PropertyName = "t")]
        public SettingTypeEnum SettingType { get; set; }

        [JsonProperty(PropertyName = "p")]
        public List<RolloutPercentageItem> RolloutPercentageItems { get; set; }

        [JsonProperty(PropertyName = "r")]
        public List<RolloutRule> RolloutRules { get; set; }

        [JsonProperty(PropertyName = "i")]
        public string VariationId { get; set; }
    }

    internal class RolloutPercentageItem
    {
        [JsonProperty(PropertyName = "o")]
        public short Order { get; set; }

        [JsonProperty(PropertyName = "v")]
        public string RawValue { get; private set; }

        [JsonProperty(PropertyName = "p")]
        public int Percentage { get; set; }

        [JsonProperty(PropertyName = "i")]
        public string VariationId { get; set; }
    }

    internal class RolloutRule
    {
        [JsonProperty(PropertyName = "o")]
        public short Order { get; set; }

        [JsonProperty(PropertyName = "a")]
        public string ComparisonAttribute { get; set; }

        [JsonProperty(PropertyName = "t")]
        public ComparatorEnum Comparator { get; set; }

        [JsonProperty(PropertyName = "c")]
        public string ComparisonValue { get; set; }

        [JsonProperty(PropertyName = "v")]
        public string RawValue { get; private set; }

        [JsonProperty(PropertyName = "i")]
        public string VariationId { get; set; }
    }

    internal enum SettingTypeEnum : byte
    {
        Boolean = 0,

        String = 1,

        Int = 2,

        Double = 3
    }

    internal enum ComparatorEnum : byte
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