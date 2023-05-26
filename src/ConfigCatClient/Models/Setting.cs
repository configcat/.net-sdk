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

/// <summary>
/// Feature flag or setting.
/// </summary>
public interface ISetting
{
    /// <summary>
    /// The (fallback) value of the setting.
    /// </summary>
    object Value { get; }

    /// <summary>
    /// Setting type.
    /// </summary>
    SettingType SettingType { get; }

    /// <summary>
    /// List of percentage options.
    /// </summary>
    IReadOnlyList<IPercentageOption> PercentageOptions { get; }

    /// <summary>
    /// List of targeting rules.
    /// </summary>
    IReadOnlyList<ITargetingRule> TargetingRules { get; }

    /// <summary>
    /// Variation ID.
    /// </summary>
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

    private IReadOnlyList<IPercentageOption>? percentageOptionsReadOnly;
    IReadOnlyList<IPercentageOption> ISetting.PercentageOptions => this.percentageOptionsReadOnly ??= new ReadOnlyCollection<RolloutPercentageItem>(RolloutPercentageItems);

    private RolloutRule[]? rolloutRules;

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

    private IReadOnlyList<ITargetingRule>? targetingRulesReadOnly;
    IReadOnlyList<ITargetingRule> ISetting.TargetingRules => this.targetingRulesReadOnly ??= new ReadOnlyCollection<RolloutRule>(RolloutRules);

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "i")]
#else
    [JsonPropertyName("i")]
#endif
    public string? VariationId { get; set; }

    [JsonIgnore]
    public string? UnsupportedTypeError { get; set; }
}
