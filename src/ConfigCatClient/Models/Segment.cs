using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a segment.
/// </summary>
public sealed class Segment
{
    [JsonConstructor]
    internal Segment() { }

    [JsonInclude, JsonPropertyName("n")]
    internal string? name;

    /// <summary>
    /// The name of the segment.
    /// </summary>
    [JsonIgnore]
    public string Name => this.name ?? throw new InvalidConfigModelException("Segment name is missing.");

    [JsonInclude, JsonPropertyName("r")]
    internal UserCondition[]? conditions;

    internal UserCondition[] ConditionsOrEmpty => this.conditions ?? Array.Empty<UserCondition>();

    private IReadOnlyList<UserCondition>? conditionsReadOnly;

    /// <summary>
    /// The list of segment rule conditions (where there is a logical AND relation between the items).
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<UserCondition> Conditions => this.conditionsReadOnly ??= this.conditions.AsReadOnly();

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSegment(this)
            .ToString();
    }
}
