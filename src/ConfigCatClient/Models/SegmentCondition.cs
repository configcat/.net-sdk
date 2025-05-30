using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a condition that is based on a segment.
/// </summary>
public interface ISegmentCondition : ICondition
{
    /// <summary>
    /// The segment that the condition is based on.
    /// </summary>
    ISegment Segment { get; }

    /// <summary>
    /// The operator which defines the expected result of the evaluation of the segment.
    /// </summary>
    SegmentComparator Comparator { get; }
}

internal sealed class SegmentCondition : Condition, ISegmentCondition
{
    public const SegmentComparator UnknownComparator = (SegmentComparator)byte.MaxValue;

    [JsonPropertyName("s")]
    public int SegmentIndex { get; set; } = -1;

    [JsonIgnore]
    public Segment? Segment { get; private set; }

    ISegment ISegmentCondition.Segment => Segment ?? throw new InvalidConfigModelException("Segment reference is invalid.");

    [JsonPropertyName("c")]
    public SegmentComparator Comparator { get; set; } = UnknownComparator;

    internal void OnConfigDeserialized(Config config)
    {
        var segments = config.Segments;
        if (0 <= SegmentIndex && SegmentIndex < segments.Length)
        {
            Segment = segments[SegmentIndex];
        }
    }

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSegmentCondition(this)
            .ToString();
    }
}
