using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

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

    [JsonPropertyName("t")]
    public SettingType SettingType { get; set; } = UnknownType;

    [JsonPropertyName("a")]
    [NotNull]
    public string? PercentageOptionsAttribute { get; set; }

    string ISetting.PercentageOptionsAttribute => PercentageOptionsAttribute ?? nameof(User.Identifier);

    private TargetingRule[]? targetingRules;

    [JsonPropertyName("r")]
    [NotNull]
    public TargetingRule[]? TargetingRules
    {
        get => this.targetingRules ?? Array.Empty<TargetingRule>();
        set => this.targetingRules = value;
    }

    private IReadOnlyList<ITargetingRule>? targetingRulesReadOnly;

    IReadOnlyList<ITargetingRule> ISetting.TargetingRules => this.targetingRulesReadOnly ??= this.targetingRules is { Length: > 0 }
        ? new ReadOnlyCollection<ITargetingRule>(this.targetingRules)
        : Array.Empty<ITargetingRule>();

    private PercentageOption[]? percentageOptions;

    [JsonPropertyName("p")]
    [NotNull]
    public PercentageOption[]? PercentageOptions
    {
        get => this.percentageOptions ?? Array.Empty<PercentageOption>();
        set => this.percentageOptions = value;
    }

    private IReadOnlyList<IPercentageOption>? percentageOptionsReadOnly;
    IReadOnlyList<IPercentageOption> ISetting.PercentageOptions => this.percentageOptionsReadOnly ??= this.percentageOptions is { Length: > 0 }
        ? new ReadOnlyCollection<IPercentageOption>(this.percentageOptions)
        : Array.Empty<IPercentageOption>();

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
