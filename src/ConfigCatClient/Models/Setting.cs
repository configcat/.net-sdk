using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ConfigCat.Client.Utils;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using System.Text.Json.Serialization;
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client;

public interface ISetting
{
    object Value { get; }
    SettingType SettingType { get; }
    IReadOnlyList<IRolloutPercentageItem> RolloutPercentageItems { get; }
    IReadOnlyList<IRolloutRule> RolloutRules { get; }
    string? VariationId { get; }
}

internal sealed class Setting : ISetting
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "v")]
#else
    [JsonPropertyName("v")]
#endif
    public JsonValue Value { get; set; } = default!;

    object ISetting.Value => Value.ConvertToObject(Value.DetermineSettingType());

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "t")]
#else
    [JsonPropertyName("t")]
#endif
    public SettingType SettingType { get; set; } = SettingType.Unknown;

    private RolloutPercentageItem[]? rolloutPercentageItems;
    private IReadOnlyList<IRolloutPercentageItem>? rolloutPercentageItemsReadOnly;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p"), JsonInclude]
#endif
    public RolloutPercentageItem[] RolloutPercentageItems
    {
        get => this.rolloutPercentageItems ??= ArrayUtils.EmptyArray<RolloutPercentageItem>();
        private set => this.rolloutPercentageItems = value;
    }

    IReadOnlyList<IRolloutPercentageItem> ISetting.RolloutPercentageItems => this.rolloutPercentageItemsReadOnly ??= new ReadOnlyCollection<RolloutPercentageItem>(RolloutPercentageItems);

    private RolloutRule[]? rolloutRules;
    private IReadOnlyList<IRolloutRule>? rolloutRulesReadOnly;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "r")]
#else
    [JsonPropertyName("r"), JsonInclude]
#endif
    public RolloutRule[] RolloutRules
    {
        get => this.rolloutRules ??= ArrayUtils.EmptyArray<RolloutRule>();
        private set => this.rolloutRules = value;
    }

    IReadOnlyList<IRolloutRule> ISetting.RolloutRules => this.rolloutRulesReadOnly ??= new ReadOnlyCollection<RolloutRule>(RolloutRules);

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "i")]
#else
    [JsonPropertyName("i")]
#endif
    public string? VariationId { get; set; }
}
