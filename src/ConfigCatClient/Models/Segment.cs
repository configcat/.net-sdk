using System;
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
/// Segment.
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
    IReadOnlyList<IComparisonCondition> Conditions { get; }
}

internal sealed class Segment : ISegment
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "n")]
#else
    [JsonPropertyName("n")]
#endif
    public string? Name { get; set; }

    string ISegment.Name => Name ?? throw new InvalidOperationException("Segment name is missing.");

    private ComparisonCondition[]? conditions;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "r")]
#else
    [JsonPropertyName("r")]
#endif
    [NotNull]
    public ComparisonCondition[]? Conditions
    {
        get => this.conditions ?? ArrayUtils.EmptyArray<ComparisonCondition>();
        set => this.conditions = value;
    }

    private IReadOnlyList<IComparisonCondition>? conditionsReadOnly;
    IReadOnlyList<IComparisonCondition> ISegment.Conditions => this.conditionsReadOnly ??= this.conditions is { Length: > 0 }
        ? new ReadOnlyCollection<IComparisonCondition>(this.conditions)
        : ArrayUtils.EmptyArray<IComparisonCondition>();

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSegment(this)
            .ToString();
    }
}
