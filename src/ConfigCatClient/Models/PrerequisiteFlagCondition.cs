using System;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Evaluation;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Describes a condition that is based on a prerequisite flag.
/// </summary>
public interface IPrerequisiteFlagCondition : ICondition
{
    /// <summary>
    /// The key of the prerequisite flag that the condition is based on.
    /// </summary>
    string PrerequisiteFlagKey { get; }

    /// <summary>
    /// The operator which defines the relation between the evaluated value of the prerequisite flag and the comparison value.
    /// </summary>
    PrerequisiteFlagComparator Comparator { get; }

    /// <summary>
    /// The value that the evaluated value of the prerequisite flag is compared to.
    /// Can be a value of the following types: <see cref="bool"/>, <see cref="string"/>, <see cref="int"/> or <see cref="double"/>.
    /// </summary>
    object ComparisonValue { get; }
}

internal sealed class PrerequisiteFlagCondition : Condition, IPrerequisiteFlagCondition
{
    public const PrerequisiteFlagComparator UnknownComparator = (PrerequisiteFlagComparator)byte.MaxValue;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "f")]
#else
    [JsonPropertyName("f")]
#endif
    public string? PrerequisiteFlagKey { get; set; }

    string IPrerequisiteFlagCondition.PrerequisiteFlagKey => PrerequisiteFlagKey ?? throw new InvalidOperationException("Prerequisite flag key is missing.");

    private PrerequisiteFlagComparator comparator = UnknownComparator;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "c")]
#else
    [JsonPropertyName("c")]
#endif
    public PrerequisiteFlagComparator Comparator
    {
        get => this.comparator;
        set => ModelHelper.SetEnum(ref this.comparator, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "v")]
#else
    [JsonPropertyName("v")]
#endif
    public SettingValue ComparisonValue { get; set; }

    object IPrerequisiteFlagCondition.ComparisonValue => ComparisonValue.GetValue()!;

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendPrerequisiteFlagCondition(this)
            .ToString();
    }
}
