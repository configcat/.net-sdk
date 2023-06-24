namespace ConfigCat.Client;

/// <summary>
/// Segment condition operator.
/// </summary>
public enum SegmentComparator : byte
{
    /// <summary>
    /// IS IN SEGMENT - Does the conditions of the specified segment evaluate to true?
    /// </summary>
    IsIn,

    /// <summary>
    /// IS NOT IN SEGMENT - Does the conditions of the specified segment evaluate to false?
    /// </summary>
    IsNotIn,
}
