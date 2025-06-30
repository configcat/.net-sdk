using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a segment.
/// </summary>
public interface ISegment
{
    /// <summary>
    /// The name of the segment.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The list of segment rule conditions (where there is a logical AND relation between the items).
    /// </summary>
    IReadOnlyList<IUserCondition> Conditions { get; }
}

internal sealed class Segment : ISegment
{
    [JsonPropertyName("n")]
    public string? Name { get; set; }

    string ISegment.Name => Name ?? throw new InvalidConfigModelException("Segment name is missing.");

    private UserCondition[]? conditions;

    [JsonPropertyName("r")]
    [NotNull]
    public UserCondition[]? Conditions
    {
        get => this.conditions ?? Array.Empty<UserCondition>();
        set => this.conditions = value;
    }

    private IReadOnlyList<IUserCondition>? conditionsReadOnly;
    IReadOnlyList<IUserCondition> ISegment.Conditions => this.conditionsReadOnly ??= this.conditions is { Length: > 0 }
        ? new ReadOnlyCollection<IUserCondition>(this.conditions)
        : Array.Empty<IUserCondition>();

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSegment(this)
            .ToString();
    }
}
