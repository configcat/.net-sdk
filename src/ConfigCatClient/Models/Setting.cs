using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a feature flag or setting.
/// </summary>
public sealed class Setting : SettingValueContainer
{
    /// <summary>
    /// Creates a setting that can be used for feature flag evaluation from the specified value.
    /// </summary>
    public static Setting FromValue(object value)
    {
        return value.ToSetting();
    }

    internal const SettingType UnknownType = (SettingType)byte.MaxValue;

    [JsonConstructor]
    internal Setting() { }

    [JsonInclude, JsonPropertyName("t")]
    internal SettingType settingType = UnknownType;

    /// <summary>
    /// Setting type.
    /// </summary>
    /// <remarks>
    /// Can also be <see cref="byte.MaxValue"/> when the setting comes from a simple flag override with an unsupported value.
    /// </remarks>
    [JsonIgnore]
    public SettingType SettingType => this.settingType;

    [JsonInclude, JsonPropertyName("a")]
    internal string? percentageOptionsAttribute;

    /// <summary>
    /// The User Object attribute which serves as the basis of percentage options evaluation.
    /// </summary>
    [JsonIgnore]
    public string PercentageOptionsAttribute => this.percentageOptionsAttribute ?? nameof(User.Identifier);

    [JsonInclude, JsonPropertyName("r")]
    internal TargetingRule[]? targetingRules;

    internal TargetingRule[] TargetingRulesOrEmpty => this.targetingRules ?? Array.Empty<TargetingRule>();

    private IReadOnlyList<TargetingRule>? targetingRulesReadOnly;

    /// <summary>
    /// The list of targeting rules (where there is a logical OR relation between the items).
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<TargetingRule> TargetingRules => this.targetingRulesReadOnly ??= this.targetingRules is { Length: > 0 }
        ? new ReadOnlyCollection<TargetingRule>(this.targetingRules)
        : Array.Empty<TargetingRule>();

    [JsonInclude, JsonPropertyName("p")]
    internal PercentageOption[]? percentageOptions;

    internal PercentageOption[] PercentageOptionsOrEmpty => this.percentageOptions ?? Array.Empty<PercentageOption>();

    private IReadOnlyList<PercentageOption>? percentageOptionsReadOnly;

    /// <summary>
    /// The list of percentage options.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<PercentageOption> PercentageOptions => this.percentageOptionsReadOnly ??= this.percentageOptions is { Length: > 0 }
        ? new ReadOnlyCollection<PercentageOption>(this.percentageOptions)
        : Array.Empty<PercentageOption>();

    internal string? configJsonSalt;

    internal void OnConfigDeserialized(Config config)
    {
        this.configJsonSalt = config.preferences?.Salt;

        foreach (var targetingRule in TargetingRulesOrEmpty)
        {
            targetingRule.OnConfigDeserialized(config);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSetting(this)
            .ToString();
    }
}
