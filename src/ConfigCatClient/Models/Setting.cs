using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Evaluation;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Feature flag or setting.
/// </summary>
public interface ISetting : ISettingValueContainer
{
    /// <summary>
    /// Setting type.
    /// </summary>
    SettingType SettingType { get; }

    /// <summary>
    /// The User Object attribute which serves as the basis of percentage options evaluation.
    /// </summary>
    string PercentageOptionsAttribute { get; }

    /// <summary>
    /// The list of targeting rules (where there is a logical OR relation between the items).
    /// </summary>
    IReadOnlyList<ITargetingRule> TargetingRules { get; }

    /// <summary>
    /// The list of percentage options.
    /// </summary>
    IReadOnlyList<IPercentageOption> PercentageOptions { get; }
}

internal sealed class Setting : SettingValueContainer, ISetting
{
    public const SettingType UnknownType = (SettingType)byte.MaxValue;

    private SettingType settingType = UnknownType;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "t")]
#else
    [JsonPropertyName("t")]
#endif
    public SettingType SettingType
    {
        get => this.settingType;
        set => ModelHelper.SetEnum(ref this.settingType, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "a")]
#else
    [JsonPropertyName("a")]
#endif
    [NotNull]
    public string? PercentageOptionsAttribute { get; set; }

    string ISetting.PercentageOptionsAttribute => PercentageOptionsAttribute ?? nameof(User.Identifier);

    private TargetingRule[]? targetingRules;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "r")]
#else
    [JsonPropertyName("r")]
#endif
    [NotNull]
    public TargetingRule[]? TargetingRules
    {
        get => this.targetingRules ?? ArrayUtils.EmptyArray<TargetingRule>();
        set => this.targetingRules = value;
    }

    private IReadOnlyList<ITargetingRule>? targetingRulesReadOnly;

    IReadOnlyList<ITargetingRule> ISetting.TargetingRules => this.targetingRulesReadOnly ??= this.targetingRules is { Length: > 0 }
        ? new ReadOnlyCollection<ITargetingRule>(this.targetingRules)
        : ArrayUtils.EmptyArray<ITargetingRule>();

    private PercentageOption[]? percentageOptions;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p")]
#endif
    [NotNull]
    public PercentageOption[]? PercentageOptions
    {
        get => this.percentageOptions ?? ArrayUtils.EmptyArray<PercentageOption>();
        set => this.percentageOptions = value;
    }

    private IReadOnlyList<IPercentageOption>? percentageOptionsReadOnly;
    IReadOnlyList<IPercentageOption> ISetting.PercentageOptions => this.percentageOptionsReadOnly ??= this.percentageOptions is { Length: > 0 }
        ? new ReadOnlyCollection<IPercentageOption>(this.percentageOptions)
        : ArrayUtils.EmptyArray<IPercentageOption>();

    [JsonIgnore]
    public string? ConfigJsonSalt { get; private set; }

    internal void OnConfigDeserialized(Config config)
    {
        ConfigJsonSalt = config.Preferences?.Salt;

        foreach (var targetingRule in TargetingRules)
        {
            targetingRule.OnConfigDeserialized(config);
        }
    }

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSetting(this)
            .ToString();
    }
}
