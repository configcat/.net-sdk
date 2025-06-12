using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

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

    [JsonPropertyName("f")]
    public string? PrerequisiteFlagKey { get; set; }

    string IPrerequisiteFlagCondition.PrerequisiteFlagKey => PrerequisiteFlagKey ?? throw new InvalidConfigModelException("Prerequisite flag key is missing.");

    [JsonPropertyName("c")]
    public PrerequisiteFlagComparator Comparator { get; set; } = UnknownComparator;

    [JsonPropertyName("v")]
    public SettingValue ComparisonValue { get; set; }

    object IPrerequisiteFlagCondition.ComparisonValue => ComparisonValue.GetValue()!;

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendPrerequisiteFlagCondition(this)
            .ToString();
    }
}
