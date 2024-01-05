namespace ConfigCat.Client;

/// <summary>
/// Segment comparison operator used during the evaluation process.
/// </summary>
public enum SegmentComparator : byte
{
    /// <summary>
    /// IS IN SEGMENT - It matches when the conditions of the specified segment are evaluated to true.
    /// </summary>
    IsIn = 0,

    /// <summary>
    /// IS NOT IN SEGMENT - It matches when the conditions of the specified segment are evaluated to false.
    /// </summary>
    IsNotIn = 1,
}
