using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Versioning;

namespace ConfigCat.Client;

/// <summary>
/// Describes a targeting rule.
/// </summary>
public sealed class TargetingRule
{
    [JsonConstructor]
    internal TargetingRule() { }

    [JsonInclude, JsonPropertyName("c")]
    internal ConditionContainer[]? conditions;

    internal ConditionContainer[] ConditionsOrEmpty => this.conditions ?? Array.Empty<ConditionContainer>();

    private IReadOnlyList<Condition>? conditionsReadOnly;

    /// <summary>
    /// The list of conditions that are combined with the AND logical operator.
    /// Items can be one of the following types: <see cref="UserCondition"/>, <see cref="SegmentCondition"/> or <see cref="PrerequisiteFlagCondition"/>.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Condition> Conditions => this.conditionsReadOnly ??= this.conditions is { Length: > 0 } conditions
        ? conditions.Select(condition => condition.GetCondition()!).ToArray()
        : Array.Empty<Condition>();

    private object? then;

    [JsonInclude, JsonPropertyName("p")]
    internal PercentageOption[]? PercentageOptionsOrNull
    {
        get => this.then as PercentageOption[];
        set => ModelHelper.SetOneOf(ref this.then, value);
    }

    private IReadOnlyList<PercentageOption>? percentageOptionsReadOnly;

    /// <summary>
    /// The list of percentage options associated with the targeting rule or <see langword="null"/> if the targeting rule has a simple value THEN part.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<PercentageOption>? PercentageOptions => this.percentageOptionsReadOnly ??= this.then is PercentageOption[] percentageOptions
        ? percentageOptions.AsReadOnly()
        : null;

    [JsonInclude, JsonPropertyName("s")]
    internal SimpleSettingValue? SimpleValueOrNull
    {
        get => this.then as SimpleSettingValue;
        set => ModelHelper.SetOneOf(ref this.then, value);
    }

    /// <summary>
    /// The simple value associated with the targeting rule or <see langword="null"/> if the targeting rule has percentage options THEN part.
    /// </summary>
    [JsonIgnore]
    public SettingValueContainer? SimpleValue => SimpleValueOrNull;

    internal void OnConfigDeserialized(Config config, ref Dictionary<string, SemVersion?>? semVerCache)
    {
        foreach (var conditionContainer in ConditionsOrEmpty)
        {
            var condition = conditionContainer.GetCondition(throwIfInvalid: false);
            if (condition is UserCondition userCondition)
            {
                userCondition.OnConfigDeserialized(ref semVerCache);
            }
            else if (condition is SegmentCondition segmentCondition)
            {
                segmentCondition.OnConfigDeserialized(config);
            }
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendTargetingRule(this)
            .ToString();
    }
}
