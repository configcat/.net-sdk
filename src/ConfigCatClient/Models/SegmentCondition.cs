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
/// Segment condition.
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

internal sealed class SegmentCondition : ISegmentCondition
{
    public const SegmentComparator UnknownComparator = (SegmentComparator)byte.MaxValue;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "s")]
#else
    [JsonPropertyName("s")]
#endif
    public int SegmentIndex { get; set; } = -1;

    [JsonIgnore]
    public Segment? Segment { get; private set; }

    ISegment ISegmentCondition.Segment => Segment ?? throw new InvalidOperationException("Segment reference is invalid.");

    private SegmentComparator comparator = UnknownComparator;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "c")]
#else
    [JsonPropertyName("c")]
#endif
    public SegmentComparator Comparator
    {
        get => this.comparator;
        set => ModelHelper.SetEnum(ref this.comparator, value);
    }

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
