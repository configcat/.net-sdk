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
public interface IUserCondition : ICondition
{
    /// <summary>
    /// The User Object attribute that the condition is based on. Can be "Identifier", "Email", "Country" or any custom attribute.
    /// </summary>
    string ComparisonAttribute { get; }

    /// <summary>
    /// The operator which defines the relation between the comparison attribute and the comparison value.
    /// </summary>
    UserComparator Comparator { get; }

    /// <summary>
    /// The value that the User Object attribute is compared to.
    /// Can be a value of the following types: <see cref="string"/> (including a semantic version), <see cref="double"/> or <see cref="IReadOnlyList{T}" /> where T is <see cref="string"/>.
    /// </summary>
    object ComparisonValue { get; }
}

internal sealed class UserCondition : Condition, IUserCondition
{
    public const UserComparator UnknownComparator = (UserComparator)byte.MaxValue;

    [JsonPropertyName("a")]
    public string? ComparisonAttribute { get; set; }

    string IUserCondition.ComparisonAttribute => ComparisonAttribute ?? throw new InvalidConfigModelException("Comparison attribute name is missing.");

    [JsonPropertyName("c")]
    public UserComparator Comparator { get; set; } = UnknownComparator;

    private object? comparisonValue;

    [JsonPropertyName("s")]
    public string? StringValue
    {
        get => this.comparisonValue as string;
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    [JsonPropertyName("d")]
    public double? DoubleValue
    {
        get => this.comparisonValue as double?;
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    [JsonPropertyName("l")]
    public string[]? StringListValue
    {
        get => this.comparisonValue as string[];
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    private object? comparisonValueReadOnly;

    object IUserCondition.ComparisonValue => this.comparisonValueReadOnly ??= GetComparisonValue() is var comparisonValue && comparisonValue is string[] stringListValue
        ? (stringListValue.Length > 0 ? new ReadOnlyCollection<string>(stringListValue) : Array.Empty<string>())
        : comparisonValue!;

    public object? GetComparisonValue(bool throwIfInvalid = true)
    {
        return ModelHelper.IsValidOneOf(this.comparisonValue)
            ? this.comparisonValue
            : (!throwIfInvalid ? null : throw new InvalidConfigModelException("Comparison value is missing or invalid."));
    }

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendUserCondition(this)
            .ToString();
    }
}
