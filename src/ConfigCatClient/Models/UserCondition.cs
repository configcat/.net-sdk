using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

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

    private object? comparisonValueReadOnly;

    /// <summary>
    /// The value that the User Object attribute is compared to.
    /// Can be a value of the following types: <see cref="string"/> (including a semantic version), <see cref="double"/> or <see cref="IReadOnlyList{T}" /> where T is <see cref="string"/>.
    /// </summary>
    [JsonIgnore]
    public object ComparisonValue => this.comparisonValueReadOnly ??= GetComparisonValue() is var comparisonValue && comparisonValue is string[] stringListValue
        ? (stringListValue.Length > 0 ? new ReadOnlyCollection<string>(stringListValue) : Array.Empty<string>())
        : comparisonValue!;

    internal object? GetComparisonValue(bool throwIfInvalid = true)
    {
        return ModelHelper.IsValidOneOf(this.comparisonValue)
            ? this.comparisonValue
            : (!throwIfInvalid ? null : throw new InvalidConfigModelException("Comparison value is missing or invalid."));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendUserCondition(this)
            .ToString();
    }
}
