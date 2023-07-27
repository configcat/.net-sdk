using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Evaluation;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Comparison condition.
/// </summary>
public interface IComparisonCondition : ICondition
{
    /// <summary>
    /// The User Object attribute that the condition is based on. Can be "User ID", "Email", "Country" or any custom attribute.
    /// </summary>
    string ComparisonAttribute { get; }

    /// <summary>
    /// The operator which defines the relation between the comparison attribute and the comparison value.
    /// </summary>
    Comparator Comparator { get; }

    /// <summary>
    /// The value that the attribute is compared to. Can be a value of the following types: <see cref="string"/> (including a semantic version), <see cref="double"/> or <see cref="IReadOnlyList{T}" />, where T is <see cref="string"/>.
    /// </summary>
    object ComparisonValue { get; }
}

internal sealed class ComparisonCondition : IComparisonCondition
{
    public const Comparator UnknownComparator = (Comparator)byte.MaxValue;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "a")]
#else
    [JsonPropertyName("a")]
#endif
    public string? ComparisonAttribute { get; set; }

    string IComparisonCondition.ComparisonAttribute => ComparisonAttribute ?? throw new InvalidOperationException("Comparison attribute name is missing.");

    private Comparator comparator = UnknownComparator;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "c")]
#else
    [JsonPropertyName("c")]
#endif
    public Comparator Comparator
    {
        get => this.comparator;
        set => ModelHelper.SetEnum(ref this.comparator, value);
    }

    private object? comparisonValue;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "s")]
#else
    [JsonPropertyName("s")]
#endif
    public string? StringValue
    {
        get => this.comparisonValue as string;
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "d")]
#else
    [JsonPropertyName("d")]
#endif
    public double? DoubleValue
    {
        get => this.comparisonValue as double?;
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "l")]
#else
    [JsonPropertyName("l")]
#endif
    public string[]? StringListValue
    {
        get => this.comparisonValue as string[];
        set => ModelHelper.SetOneOf(ref this.comparisonValue, value);
    }

    private object? comparisonValueReadOnly;

    object IComparisonCondition.ComparisonValue => this.comparisonValueReadOnly ??= GetComparisonValue() is var comparisonValue && comparisonValue is string[] stringListValue
        ? (stringListValue.Length > 0 ? new ReadOnlyCollection<string>(stringListValue) : ArrayUtils.EmptyArray<string>())
        : comparisonValue!;

    public object? GetComparisonValue(bool throwIfInvalid = true)
    {
        return ModelHelper.IsValidOneOf(this.comparisonValue)
            ? this.comparisonValue
            : (!throwIfInvalid ? null : throw new InvalidOperationException("Comparison value is missing or invalid."));
    }

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendComparisonCondition(this)
            .ToString();
    }
}
