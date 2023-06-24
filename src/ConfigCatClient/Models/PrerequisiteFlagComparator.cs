namespace ConfigCat.Client;

/// <summary>
/// Prerequisite flag condition operator.
/// </summary>
public enum PrerequisiteFlagComparator : byte
{
    /// <summary>
    /// EQUALS - Is the evaluated value of the specified prerequisite flag equal to the comparison value?
    /// </summary>
    Equals = 0,

    /// <summary>
    /// NOT EQUALS - Is the evaluated value of the specified prerequisite flag not equal to the comparison value?
    /// </summary>
    NotEquals = 1
}
