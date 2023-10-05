namespace ConfigCat.Client;

/// <summary>
/// User Object attribute comparison operator used during the evaluation process.
/// </summary>
public enum UserComparator : byte
{
    /// <summary>
    /// CONTAINS ANY OF - It matches when the comparison attribute contains any comparison values as a substring.
    /// </summary>
    Contains = 2,

    /// <summary>
    /// NOT CONTAINS ANY OF - It matches when the comparison attribute does not contain any comparison values as a substring.
    /// </summary>
    NotContains = 3,

    /// <summary>
    /// IS ONE OF (semver) - It matches when the comparison attribute interpreted as a semantic version is equal to any of the comparison values.
    /// </summary>
    SemVerOneOf = 4,

    /// <summary>
    /// IS NOT ONE OF (semver) - It matches when the comparison attribute interpreted as a semantic version is not equal to any of the comparison values.
    /// </summary>
    SemVerNotOneOf = 5,

    /// <summary>
    /// &lt; (semver) - It matches when the comparison attribute interpreted as a semantic version is less than the comparison value.
    /// </summary>
    SemVerLessThan = 6,

    /// <summary>
    /// &lt;= (semver) - It matches when the comparison attribute interpreted as a semantic version is less than or equal to the comparison value.
    /// </summary>
    SemVerLessThanEqual = 7,

    /// <summary>
    /// &gt; (semver) - It matches when the comparison attribute interpreted as a semantic version is greater than the comparison value.
    /// </summary>
    SemVerGreaterThan = 8,

    /// <summary>
    /// &gt;= (semver) - It matches when the comparison attribute interpreted as a semantic version is greater than or equal to the comparison value.
    /// </summary>
    SemVerGreaterThanEqual = 9,

    /// <summary>
    /// = (number) - It matches when the comparison attribute interpreted as a decimal number is equal to the comparison value.
    /// </summary>
    NumberEqual = 10,

    /// <summary>
    /// != (number) - It matches when the comparison attribute interpreted as a decimal number is not equal to the comparison value.
    /// </summary>
    NumberNotEqual = 11,

    /// <summary>
    /// &lt; (number) - It matches when the comparison attribute interpreted as a decimal number is less than the comparison value.
    /// </summary>
    NumberLessThan = 12,

    /// <summary>
    /// &lt;= (number) - It matches when the comparison attribute interpreted as a decimal number is less than or equal to the comparison value.
    /// </summary>
    NumberLessThanEqual = 13,

    /// <summary>
    /// &gt; (number) - It matches when the comparison attribute interpreted as a decimal number is greater than the comparison value.
    /// </summary>
    NumberGreaterThan = 14,

    /// <summary>
    /// &gt;= (number) - It matches when the comparison attribute interpreted as a decimal number is greater than or equal to the comparison value.
    /// </summary>
    NumberGreaterThanEqual = 15,

    /// <summary>
    /// IS ONE OF (hashed) - It matches when the comparison attribute is equal to any of the comparison values (where the comparison is performed using the SHA256 hashes of the values).
    /// </summary>
    SensitiveOneOf = 16,

    /// <summary>
    /// IS NOT ONE OF (hashed) - It matches when the comparison attribute is not equal to any of the comparison values (where the comparison is performed using the SHA256 hashes of the values).
    /// </summary>
    SensitiveNotOneOf = 17,

    /// <summary>
    /// BEFORE (UTC datetime) - It matches when the comparison attribute interpreted as the seconds elapsed since <see href="https://en.wikipedia.org/wiki/Unix_time">Unix Epoch</see> is less than the comparison value.
    /// </summary>
    DateTimeBefore = 18,

    /// <summary>
    /// AFTER (UTC datetime) - It matches when the comparison attribute interpreted as the seconds elapsed since <see href="https://en.wikipedia.org/wiki/Unix_time">Unix Epoch</see> is greater than the comparison value.
    /// </summary>
    DateTimeAfter = 19,

    /// <summary>
    /// EQUALS (hashed) - It matches when the comparison attribute is equal to the comparison value (where the comparison is performed using the SHA256 hashes of the values).
    /// </summary>
    SensitiveTextEquals = 20,

    /// <summary>
    /// NOT EQUALS (hashed) - It matches when the comparison attribute is not equal to the comparison value (where the comparison is performed using the SHA256 hashes of the values).
    /// </summary>
    SensitiveTextNotEquals = 21,

    /// <summary>
    /// STARTS WITH ANY OF (hashed) - It matches when the comparison attribute starts with any of the comparison values (where the comparison is performed using the SHA256 hashes of the values).
    /// </summary>
    SensitiveTextStartsWith = 22,

    /// <summary>
    /// NOT STARTS WITH ANY OF (hashed) - It matches when the comparison attribute does not start with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextNotStartsWith = 23,

    /// <summary>
    /// ENDS WITH ANY OF (hashed) - It matches when the comparison attribute ends with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextEndsWith = 24,

    /// <summary>
    /// NOT ENDS WITH ANY OF (hashed) - It matches when the comparison attribute does not end with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextNotEndsWith = 25,

    /// <summary>
    /// ARRAY CONTAINS ANY OF (hashed) - It matches when the comparison attribute interpreted as a comma-separated list contains any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveArrayContains = 26,

    /// <summary>
    /// ARRAY NOT CONTAINS ANY OF (hashed) - It matches when the comparison attribute interpreted as a comma-separated list does not contain any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveArrayNotContains = 27,
}
