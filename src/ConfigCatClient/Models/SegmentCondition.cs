using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a condition that is based on a segment.
/// </summary>
public sealed class SegmentCondition : Condition
{
    internal const SegmentComparator UnknownComparator = (SegmentComparator)byte.MaxValue;

    [JsonConstructor]
    internal SegmentCondition() { }

    [JsonInclude, JsonPropertyName("s")]
    internal int segmentIndex = -1;

    internal Segment? segment;

    /// <summary>
    /// The segment that the condition is based on.
    /// </summary>
    [JsonIgnore]
    public Segment Segment => this.segment ?? throw new InvalidConfigModelException("Segment reference is invalid.");

    [JsonInclude, JsonPropertyName("c")]
    internal SegmentComparator comparator = UnknownComparator;

    /// <summary>
    /// The operator which defines the expected result of the evaluation of the segment.
    /// </summary>
    [JsonIgnore]
    public SegmentComparator Comparator => this.comparator;

    internal void OnConfigDeserialized(Config config)
    {
        var segments = config.SegmentsOrEmpty;
        if (0 <= this.segmentIndex && this.segmentIndex < segments.Length)
        {
            this.segment = segments[this.segmentIndex];
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendSegmentCondition(this)
            .ToString();
    }
}
