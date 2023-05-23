namespace ConfigCat.Client;

/// <summary>
/// Targeting rule comparison operator.
/// </summary>
public enum Comparator : byte
{
    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of strings contain the comparison attribute?
    /// </summary>
    In = 0,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of strings not contain the comparison attribute?
    /// </summary>
    NotIn = 1,

    /// <summary>
    /// Does the comparison value contain the comparison attribute as a substring?
    /// </summary>
    Contains = 2,

    /// <summary>
    /// Does the comparison value not contain the comparison attribute as a substring?
    /// </summary>
    NotContains = 3,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of semantic versions contain the comparison attribute?
    /// </summary>
    SemVerIn = 4,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of semantic versions not contain the comparison attribute?
    /// </summary>
    SemVerNotIn = 5,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version less than the comparison attribute?
    /// </summary>
    SemVerLessThan = 6,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version less than or equal to the comparison attribute?
    /// </summary>
    SemVerLessThanEqual = 7,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version greater than the comparison attribute?
    /// </summary>
    SemVerGreaterThan = 8,

    /// <summary>
    /// Is the comparison value interpreted as a semantic version greater than or equal to the comparison attribute?
    /// </summary>
    SemVerGreaterThanEqual = 9,

    /// <summary>
    /// Is the comparison value interpreted as a number equal to the comparison attribute?
    /// </summary>
    NumberEqual = 10,

    /// <summary>
    /// Is the comparison value interpreted as a number not equal to the comparison attribute?
    /// </summary>
    NumberNotEqual = 11,

    /// <summary>
    /// Is the comparison value interpreted as a number less than the comparison attribute?
    /// </summary>
    NumberLessThan = 12,

    /// <summary>
    /// Is the comparison value interpreted as a number less than or equal to the comparison attribute?
    /// </summary>
    NumberLessThanEqual = 13,

    /// <summary>
    /// Is the comparison value interpreted as a number greater than the comparison attribute?
    /// </summary>
    NumberGreaterThan = 14,

    /// <summary>
    /// Is the comparison value interpreted as a number greater than or equal to the comparison attribute?
    /// </summary>
    NumberGreaterThanEqual = 15,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of hashes of strings contain the hash of the comparison attribute?
    /// </summary>
    SensitiveOneOf = 16,

    /// <summary>
    /// Does the comparison value interpreted as a comma-separated list of hashes of strings not contain the hash of the comparison attribute?
    /// </summary>
    SensitiveNotOneOf = 17
}
