using System.Collections.Generic;
using Newtonsoft.Json;

namespace ConfigCat.Client.Evaluate
{
    internal class Setting
    {
        public string Value { get; set; }

        public SettingTypeEnum SettingType { get; set; }

        public List<RolloutPercentageItem> RolloutPercentageItems { get; set; }

        public List<RolloutRule> RolloutRules { get; set; }
    }

    internal class RolloutPercentageItem
    {
        public short Order { get; set; }

        [JsonProperty(PropertyName = "Value")]
        public string RawValue { get; private set; }

        public int Percentage { get; set; }        
    }

    internal class RolloutRule
    {
        public short Order { get; set; }

        public string ComparisonAttribute { get; set; }

        public ComparatorEnum Comparator { get; set; }

        public string ComparisonValue { get; set; }

        [JsonProperty(PropertyName = "Value")]
        public string RawValue { get; private set; }
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

        NotContains = 3
    }
}