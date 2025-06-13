using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a condition that is based on a prerequisite flag.
/// </summary>
public sealed class PrerequisiteFlagCondition : Condition
{
    internal const PrerequisiteFlagComparator UnknownComparator = (PrerequisiteFlagComparator)byte.MaxValue;

    [JsonConstructor]
    internal PrerequisiteFlagCondition() { }

    [JsonInclude, JsonPropertyName("f")]
    internal string? prerequisiteFlagKey;

    /// <summary>
    /// The key of the prerequisite flag that the condition is based on.
    /// </summary>
    [JsonIgnore]
    public string PrerequisiteFlagKey => this.prerequisiteFlagKey ?? throw new InvalidConfigModelException("Prerequisite flag key is missing.");

    [JsonInclude, JsonPropertyName("c")]
    internal PrerequisiteFlagComparator comparator = UnknownComparator;

    /// <summary>
    /// The operator which defines the relation between the evaluated value of the prerequisite flag and the comparison value.
    /// </summary>
    [JsonIgnore]
    public PrerequisiteFlagComparator Comparator => this.comparator;

    [JsonInclude, JsonPropertyName("v")]
    internal SettingValue comparisonValue;

    /// <summary>
    /// The value that the evaluated value of the prerequisite flag is compared to.
    /// Can be a value of the following types: <see cref="bool"/>, <see cref="string"/>, <see cref="int"/> or <see cref="double"/>.
    /// </summary>
    [JsonIgnore]
    public object ComparisonValue => this.comparisonValue.GetValue()!;

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendPrerequisiteFlagCondition(this)
            .ToString();
    }
}
