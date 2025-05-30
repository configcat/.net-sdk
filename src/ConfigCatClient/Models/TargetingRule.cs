using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a targeting rule.
/// </summary>
public interface ITargetingRule
{
    /// <summary>
    /// The list of conditions that are combined with the AND logical operator.
    /// Items can be one of the following types: <see cref="IUserCondition"/>, <see cref="ISegmentCondition"/> or <see cref="IPrerequisiteFlagCondition"/>.
    /// </summary>
    IReadOnlyList<ICondition> Conditions { get; }

    /// <summary>
    /// The list of percentage options associated with the targeting rule or <see langword="null"/> if the targeting rule has a simple value THEN part.
    /// </summary>
    IReadOnlyList<IPercentageOption>? PercentageOptions { get; }

    /// <summary>
    /// The simple value associated with the targeting rule or <see langword="null"/> if the targeting rule has percentage options THEN part.
    /// </summary>
    ISettingValueContainer? SimpleValue { get; }
}

internal sealed class TargetingRule : ITargetingRule
{
    private ConditionContainer[]? conditions;

    [JsonPropertyName("c")]
    [NotNull]
    public ConditionContainer[]? Conditions
    {
        get => this.conditions ?? Array.Empty<ConditionContainer>();
        set => this.conditions = value;
    }

    private IReadOnlyList<ICondition>? conditionsReadOnly;
    IReadOnlyList<ICondition> ITargetingRule.Conditions => this.conditionsReadOnly ??= this.conditions is { Length: > 0 } conditions
        ? conditions.Select(condition => condition.GetCondition()!).ToArray()
        : Array.Empty<ICondition>();

    private object? then;

    [JsonPropertyName("p")]
    public PercentageOption[]? PercentageOptions
    {
        get => this.then as PercentageOption[];
        set => ModelHelper.SetOneOf(ref this.then, value);
    }

    private IReadOnlyList<IPercentageOption>? percentageOptionsReadOnly;
    IReadOnlyList<IPercentageOption>? ITargetingRule.PercentageOptions => this.percentageOptionsReadOnly ??= this.then is PercentageOption[] percentageOptions
        ? (percentageOptions.Length > 0 ? new ReadOnlyCollection<IPercentageOption>(percentageOptions) : Array.Empty<IPercentageOption>())
        : null;

    [JsonPropertyName("s")]
    public SimpleSettingValue? SimpleValue
    {
        get => this.then as SimpleSettingValue;
        set => ModelHelper.SetOneOf(ref this.then, value);
    }

    ISettingValueContainer? ITargetingRule.SimpleValue => SimpleValue;

    internal void OnConfigDeserialized(Config config)
    {
        foreach (var condition in Conditions)
        {
            if (condition.GetCondition(throwIfInvalid: false) is SegmentCondition segmentCondition)
            {
                segmentCondition.OnConfigDeserialized(config);
            }
        }
    }

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendTargetingRule(this)
            .ToString();
    }
}
