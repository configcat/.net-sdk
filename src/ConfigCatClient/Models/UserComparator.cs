namespace ConfigCat.Client;

/// <summary>
/// User Object attribute comparison operator used during the evaluation process.
/// </summary>
public enum UserComparator : byte
{
    /// <summary>
    /// IS ONE OF (cleartext) - It matches when the comparison attribute is equal to any of the comparison values.
    /// </summary>
    IsOneOf = 0,

    /// <summary>
    /// IS NOT ONE OF (cleartext) - It matches when the comparison attribute is not equal to any of the comparison values.
    /// </summary>
    IsNotOneOf = 1,

    /// <summary>
    /// CONTAINS ANY OF (cleartext) - It matches when the comparison attribute contains any comparison values as a substring.
    /// </summary>
    ContainsAnyOf = 2,

    /// <summary>
    /// NOT CONTAINS ANY OF (cleartext) - It matches when the comparison attribute does not contain any comparison values as a substring.
    /// </summary>
    NotContainsAnyOf = 3,

    /// <summary>
    /// IS ONE OF (semver) - It matches when the comparison attribute interpreted as a semantic version is equal to any of the comparison values.
    /// </summary>
    SemVerIsOneOf = 4,

    /// <summary>
    /// IS NOT ONE OF (semver) - It matches when the comparison attribute interpreted as a semantic version is not equal to any of the comparison values.
    /// </summary>
    SemVerIsNotOneOf = 5,

    /// <summary>
    /// &lt; (semver) - It matches when the comparison attribute interpreted as a semantic version is less than the comparison value.
    /// </summary>
    SemVerLess = 6,

    /// <summary>
    /// &lt;= (semver) - It matches when the comparison attribute interpreted as a semantic version is less than or equal to the comparison value.
    /// </summary>
    SemVerLessOrEquals = 7,

    /// <summary>
    /// &gt; (semver) - It matches when the comparison attribute interpreted as a semantic version is greater than the comparison value.
    /// </summary>
    SemVerGreater = 8,

    /// <summary>
    /// &gt;= (semver) - It matches when the comparison attribute interpreted as a semantic version is greater than or equal to the comparison value.
    /// </summary>
    SemVerGreaterOrEquals = 9,

    /// <summary>
    /// = (number) - It matches when the comparison attribute interpreted as a decimal number is equal to the comparison value.
    /// </summary>
    NumberEquals = 10,

    /// <summary>
    /// != (number) - It matches when the comparison attribute interpreted as a decimal number is not equal to the comparison value.
    /// </summary>
    NumberNotEquals = 11,

    /// <summary>
    /// &lt; (number) - It matches when the comparison attribute interpreted as a decimal number is less than the comparison value.
    /// </summary>
    NumberLess = 12,

    /// <summary>
    /// &lt;= (number) - It matches when the comparison attribute interpreted as a decimal number is less than or equal to the comparison value.
    /// </summary>
    NumberLessOrEquals = 13,

    /// <summary>
    /// &gt; (number) - It matches when the comparison attribute interpreted as a decimal number is greater than the comparison value.
    /// </summary>
    NumberGreater = 14,

    /// <summary>
    /// &gt;= (number) - It matches when the comparison attribute interpreted as a decimal number is greater than or equal to the comparison value.
    /// </summary>
    NumberGreaterOrEquals = 15,

    /// <summary>
    /// IS ONE OF (hashed) - It matches when the comparison attribute is equal to any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveIsOneOf = 16,

    /// <summary>
    /// IS NOT ONE OF (hashed) - It matches when the comparison attribute is not equal to any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveIsNotOneOf = 17,

    /// <summary>
    /// BEFORE (UTC datetime) - It matches when the comparison attribute interpreted as the seconds elapsed since <see href="https://en.wikipedia.org/wiki/Unix_time">Unix Epoch</see> is less than the comparison value.
    /// </summary>
    DateTimeBefore = 18,

    /// <summary>
    /// AFTER (UTC datetime) - It matches when the comparison attribute interpreted as the seconds elapsed since <see href="https://en.wikipedia.org/wiki/Unix_time">Unix Epoch</see> is greater than the comparison value.
    /// </summary>
    DateTimeAfter = 19,

    /// <summary>
    /// EQUALS (hashed) - It matches when the comparison attribute is equal to the comparison value (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextEquals = 20,

    /// <summary>
    /// NOT EQUALS (hashed) - It matches when the comparison attribute is not equal to the comparison value (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextNotEquals = 21,

    /// <summary>
    /// STARTS WITH ANY OF (hashed) - It matches when the comparison attribute starts with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextStartsWithAnyOf = 22,

    /// <summary>
    /// NOT STARTS WITH ANY OF (hashed) - It matches when the comparison attribute does not start with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextNotStartsWithAnyOf = 23,

    /// <summary>
    /// ENDS WITH ANY OF (hashed) - It matches when the comparison attribute ends with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextEndsWithAnyOf = 24,

    /// <summary>
    /// NOT ENDS WITH ANY OF (hashed) - It matches when the comparison attribute does not end with any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveTextNotEndsWithAnyOf = 25,

    /// <summary>
    /// ARRAY CONTAINS ANY OF (hashed) - It matches when the comparison attribute interpreted as a comma-separated list contains any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveArrayContainsAnyOf = 26,

    /// <summary>
    /// ARRAY NOT CONTAINS ANY OF (hashed) - It matches when the comparison attribute interpreted as a comma-separated list does not contain any of the comparison values (where the comparison is performed using the salted SHA256 hashes of the values).
    /// </summary>
    SensitiveArrayNotContainsAnyOf = 27,

    /// <summary>
    /// EQUALS (cleartext) - It matches when the comparison attribute is equal to the comparison value.
    /// </summary>
    TextEquals = 28,

    /// <summary>
    /// NOT EQUALS (cleartext) - It matches when the comparison attribute is not equal to the comparison value.
    /// </summary>
    TextNotEquals = 29,

    /// <summary>
    /// STARTS WITH ANY OF (cleartext) - It matches when the comparison attribute starts with any of the comparison values.
    /// </summary>
    TextStartsWithAnyOf = 30,

    /// <summary>
    /// NOT STARTS WITH ANY OF (cleartext) - It matches when the comparison attribute does not start with any of the comparison values.
    /// </summary>
    TextNotStartsWithAnyOf = 31,

    /// <summary>
    /// ENDS WITH ANY OF (cleartext) - It matches when the comparison attribute ends with any of the comparison values.
    /// </summary>
    TextEndsWithAnyOf = 32,

    /// <summary>
    /// NOT ENDS WITH ANY OF (cleartext) - It matches when the comparison attribute does not end with any of the comparison values.
    /// </summary>
    TextNotEndsWithAnyOf = 33,

    /// <summary>
    /// ARRAY CONTAINS ANY OF (cleartext) - It matches when the comparison attribute interpreted as a comma-separated list contains any of the comparison values.
    /// </summary>
    ArrayContainsAnyOf = 34,

    /// <summary>
    /// ARRAY NOT CONTAINS ANY OF (cleartext) - It matches when the comparison attribute interpreted as a comma-separated list does not contain any of the comparison values.
    /// </summary>
    ArrayNotContainsAnyOf = 35,
}
