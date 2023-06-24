namespace ConfigCat.Client;

/// <summary>
/// Comparison condition operator.
/// </summary>
public enum Comparator : byte
{
    /// <summary>
    /// CONTAINS ANY OF - Does the comparison attribute contain any of the comparison values as a substring?
    /// </summary>
    Contains = 2,

    /// <summary>
    /// NOT CONTAINS ANY OF - Does the comparison attribute not contain any of the comparison values as a substring?
    /// </summary>
    NotContains = 3,

    /// <summary>
    /// IS ONE OF (semver) - Is the comparison attribute interpreted as a semantic version equal to any of the comparison values?
    /// </summary>
    SemVerOneOf = 4,

    /// <summary>
    /// IS NOT ONE OF (semver) - Is the comparison attribute interpreted as a semantic version not equal to any of the comparison values?
    /// </summary>
    SemVerNotOneOf = 5,

    /// <summary>
    /// &lt; (semver) - Is the comparison attribute interpreted as a semantic version less than the comparison value?
    /// </summary>
    SemVerLessThan = 6,

    /// <summary>
    /// &lt;= (semver) - Is the comparison attribute interpreted as a semantic version less than or equal to the comparison value?
    /// </summary>
    SemVerLessThanEqual = 7,

    /// <summary>
    /// &gt; (semver) - Is the comparison attribute interpreted as a semantic version greater than the comparison value?
    /// </summary>
    SemVerGreaterThan = 8,

    /// <summary>
    /// &gt;= (semver) - Is the comparison attribute interpreted as a semantic version greater than or equal to the comparison value?
    /// </summary>
    SemVerGreaterThanEqual = 9,

    /// <summary>
    /// = (number) - Is the comparison attribute interpreted as a decimal number equal to the comparison value?
    /// </summary>
    NumberEqual = 10,

    /// <summary>
    /// != (number) - Is the comparison attribute interpreted as a decimal number not equal to the comparison value?
    /// </summary>
    NumberNotEqual = 11,

    /// <summary>
    /// &lt; (number)  - Is the comparison attribute interpreted as a decimal number less than the comparison value?
    /// </summary>
    NumberLessThan = 12,

    /// <summary>
    /// &lt;= (number) - Is the comparison attribute interpreted as a decimal number less than or equal to the comparison value?
    /// </summary>
    NumberLessThanEqual = 13,

    /// <summary>
    /// &gt; (number) - Is the comparison attribute interpreted as a decimal number greater than the comparison value?
    /// </summary>
    NumberGreaterThan = 14,

    /// <summary>
    /// &gt;= (number) - Is the comparison attribute interpreted as a decimal number greater than or equal to the comparison value?
    /// </summary>
    NumberGreaterThanEqual = 15,

    /// <summary>
    /// IS ONE OF (hashed) - Is the comparison attribute equal to any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveOneOf = 16,

    /// <summary>
    /// IS NOT ONE OF (hashed) - Is the comparison attribute not equal to any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveNotOneOf = 17,

    /// <summary>
    /// BEFORE (UTC datetime) - Is the comparison attribute interpreted as the seconds elapsed since <see href="https://en.wikipedia.org/wiki/Unix_time">Unix Epoch</see> less than the comparison value?
    /// </summary>
    DateTimeBefore = 18,

    /// <summary>
    /// AFTER (UTC datetime) - Is the comparison attribute interpreted as the seconds elapsed since <see href="https://en.wikipedia.org/wiki/Unix_time">Unix Epoch</see> greater than the comparison value?
    /// </summary>
    DateTimeAfter = 19,

    /// <summary>
    /// EQUALS (hashed) - Is the comparison attribute equal to the comparison value (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveTextEquals = 20,

    /// <summary>
    /// NOT EQUALS (hashed) - Is the comparison attribute not equal to the comparison value (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveTextNotEquals = 21,

    /// <summary>
    /// STARTS WITH ANY OF (hashed) - Does the comparison attribute start with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveTextStartsWith = 22,

    /// <summary>
    /// NOT STARTS WITH ANY OF (hashed) - Does the comparison attribute not start with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveTextNotStartsWith = 23,

    /// <summary>
    /// ENDS WITH ANY OF (hashed) - Does the comparison attribute end with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveTextEndsWith = 24,

    /// <summary>
    /// NOT ENDS WITH ANY OF (hashed) - Does the comparison attribute not end with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveTextNotEndsWith = 25,

    /// <summary>
    /// ARRAY CONTAINS (hashed) - Does the comparison attribute interpreted as a comma-separated list contain the comparison value (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveArrayContains = 26,

    /// <summary>
    /// ARRAY NOT CONTAINS (hashed) - Does the comparison attribute interpreted as a comma-separated list contain the comparison value (where the comparison is performed using the salted SHA256 hashes of the values)?
    /// </summary>
    SensitiveArrayNotContains = 27,
}
