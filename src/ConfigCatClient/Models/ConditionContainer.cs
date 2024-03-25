using System;
using ConfigCat.Client.Utils;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

internal struct ConditionContainer : IConditionProvider
{
    private object? condition;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "u")]
#else
    [JsonPropertyName("u")]
#endif
    public UserCondition? UserCondition
    {
        readonly get => this.condition as UserCondition;
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
    [JsonProperty(PropertyName = "p")]
#else
    [JsonPropertyName("p")]
#endif
    public PrerequisiteFlagCondition? PrerequisiteFlagCondition
    {
        readonly get => this.condition as PrerequisiteFlagCondition;
        set => ModelHelper.SetOneOf(ref this.condition, value);
    }

    public readonly Condition? GetCondition(bool throwIfInvalid = true)
    {
        return this.condition as Condition
            ?? (!throwIfInvalid ? null : throw new InvalidConfigModelException("Condition is missing or invalid."));
    }
}
