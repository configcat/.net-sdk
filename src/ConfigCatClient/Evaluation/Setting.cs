﻿using System.Collections.Generic;
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

    internal enum SettingType : byte
    {
        Boolean = 0,
        String = 1,
        Int = 2,
        Double = 3
    }

    internal enum RedirectMode : byte
    {
        No = 0,
        Should = 1,
        Force = 2
    }
}