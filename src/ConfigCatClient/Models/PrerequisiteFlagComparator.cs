namespace ConfigCat.Client;

/// <summary>
/// Prerequisite flag comparison operator used during the evaluation process.
/// </summary>
public enum PrerequisiteFlagComparator : byte
{
    /// <summary>
    /// EQUALS - It matches when the evaluated value of the specified prerequisite flag is equal to the comparison value.
    /// </summary>
    Equals = 0,

    /// <summary>
    /// NOT EQUALS - It matches when the evaluated value of the specified prerequisite flag is not equal to the comparison value.
    /// </summary>
    NotEquals = 1
}
