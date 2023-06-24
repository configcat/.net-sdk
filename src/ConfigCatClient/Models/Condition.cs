using System;
using ConfigCat.Client.Utils;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// Base interface for conditions.
/// </summary>
public interface ICondition { }

internal struct ConditionWrapper
{
    private object? condition;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "t")]
#else
    [JsonPropertyName("t")]
#endif
    public ComparisonCondition? ComparisonCondition
    {
        readonly get => this.condition as ComparisonCondition;
        set => ModelHelper.SetOneOf(ref this.condition, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "s")]
#else
    [JsonPropertyName("s")]
#endif
    public SegmentCondition? SegmentCondition
    {
        readonly get => this.condition as SegmentCondition;
        set => ModelHelper.SetOneOf(ref this.condition, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "d")]
#else
    [JsonPropertyName("d")]
#endif
    public PrerequisiteFlagCondition? PrerequisiteFlagCondition
    {
        readonly get => this.condition as PrerequisiteFlagCondition;
        set => ModelHelper.SetOneOf(ref this.condition, value);
    }

    public readonly ICondition? GetCondition(bool throwIfInvalid = true)
    {
        return this.condition as ICondition
            ?? (!throwIfInvalid ? null : throw new InvalidOperationException("Condition is missing or invalid."));
    }
}
