using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Versioning;

namespace ConfigCat.Client;

/// <summary>
/// Describes a condition that is based on a User Object attribute.
/// </summary>
public sealed class UserCondition : Condition
{
    internal const UserComparator UnknownComparator = (UserComparator)byte.MaxValue;

    [JsonConstructor]
    internal UserCondition() { }

    [JsonInclude, JsonPropertyName("a")]
    internal string? comparisonAttribute;

    /// <summary>
    /// The User Object attribute that the condition is based on. Can be "Identifier", "Email", "Country" or any custom attribute.
    /// </summary>
    [JsonIgnore]
    public string ComparisonAttribute => this.comparisonAttribute ?? throw new InvalidConfigModelException("Comparison attribute name is missing.");

    [JsonInclude, JsonPropertyName("c")]
    internal UserComparator comparator = UnknownComparator;

    /// <summary>
    /// The operator which defines the relation between the comparison attribute and the comparison value.
    /// </summary>
    [JsonIgnore]
    public UserComparator Comparator => this.comparator;

    private object? comparisonValue;

    [JsonInclude, JsonPropertyName("s")]
    internal string? StringValue
    {
        get => this.comparisonValue as string;
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    [JsonInclude, JsonPropertyName("d")]
    internal double? DoubleValue
    {
        get => this.comparisonValue as double?;
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    [JsonInclude, JsonPropertyName("l")]
    internal string[]? StringListValue
    {
        get => this.comparisonValue as string[];
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    internal SemVersion? SemVerValue => this.comparisonValueReadOnly as SemVersion;

    internal SemVersion?[]? SemVerListValue => this.comparisonValueReadOnly is SemVerList semVerList ? semVerList.Parsed : null;

    private object? comparisonValueReadOnly; // also used for storing a preparsed value (StrongBox<SemVersion?> or SemVerList)

    /// <summary>
    /// The value that the User Object attribute is compared to.
    /// Can be a value of the following types: <see cref="string"/> (including a semantic version), <see cref="double"/> or <see cref="IReadOnlyList{T}" /> where T is <see cref="string"/>.
    /// </summary>
    [JsonIgnore]
    public object ComparisonValue
    {
        get
        {
            switch (this.comparisonValueReadOnly)
            {
                case null:
                    var comparisonValue = GetComparisonValue();
                    return this.comparisonValueReadOnly = comparisonValue is string[] stringListValue
                        ? (stringListValue.Length > 0 ? new ReadOnlyCollection<string>(stringListValue) : Array.Empty<string>())
                        : comparisonValue!;
                case SemVersion:
                    return this.comparisonValue!;
                case SemVerList semVerList:
                    stringListValue = (string[])this.comparisonValue!;
                    return semVerList.ReadOnly ??=
                        stringListValue.Length > 0 ? new ReadOnlyCollection<string>(stringListValue) : Array.Empty<string>();
                default:
                    return this.comparisonValueReadOnly;
            }
        }
    }

    internal object? GetComparisonValue(bool throwIfInvalid = true)
    {
        return ModelHelper.IsValidOneOf(this.comparisonValue)
            ? this.comparisonValue
            : (!throwIfInvalid ? null : throw new InvalidConfigModelException("Comparison value is missing or invalid."));
    }

    internal void OnConfigDeserialized(ref Dictionary<string, SemVersion?>? semVerCache)
    {
        // NOTE: We preparse version and version list comparison values to improve feature flag evaluation performance
        // (both execution time and memory allocation).

        switch (Comparator)
        {
            case UserComparator.SemVerIsOneOf:
            case UserComparator.SemVerIsNotOneOf:
                if (GetComparisonValue(throwIfInvalid: false) is string[] stringListValue)
                {
                    var parsedSemVers = new SemVersion?[stringListValue.Length];
                    for (var i = 0; i < stringListValue.Length; i++)
                    {
                        parsedSemVers[i] = stringListValue[i] is { } value
                            ? GetOrParseVersion(value, ref semVerCache)
                            : null;
                    }
                    this.comparisonValueReadOnly = new SemVerList(parsedSemVers);
                }
                break;
            case UserComparator.SemVerLess:
            case UserComparator.SemVerLessOrEquals:
            case UserComparator.SemVerGreater:
            case UserComparator.SemVerGreaterOrEquals:
                if (GetComparisonValue(throwIfInvalid: false) is string stringValue)
                {
                    this.comparisonValueReadOnly = GetOrParseVersion(stringValue, ref semVerCache);
                }
                break;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendUserCondition(this)
            .ToString();
    }

    private static SemVersion? GetOrParseVersion(string value, ref Dictionary<string, SemVersion?>? semVerCache)
    {
        SemVersion? semVer;

        if (semVerCache is null)
        {
            semVerCache = new Dictionary<string, SemVersion?>();
        }
        else if (semVerCache.TryGetValue(value, out semVer))
        {
            return semVer;
        }

        if (!SemVersion.TryParse(value.Trim(), out semVer, strict: true))
        {
            semVer = null;
        }

        semVerCache[value] = semVer;
        return semVer;
    }

    private sealed class SemVerList
    {
        public SemVerList(SemVersion?[] parsed)
        {
            this.Parsed = parsed;
        }

        public readonly SemVersion?[] Parsed;
        public IReadOnlyCollection<string>? ReadOnly;
    }
}
