using System.Text.Json.Serialization;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

internal struct ConditionContainer : IConditionProvider
{
    private object? condition;

    [JsonPropertyName("u")]
    public UserCondition? UserCondition
    {
        readonly get => this.condition as UserCondition;
        set => ModelHelper.SetOneOf(ref this.condition, value);
    }

    [JsonPropertyName("s")]
    public SegmentCondition? SegmentCondition
    {
        readonly get => this.condition as SegmentCondition;
        set => ModelHelper.SetOneOf(ref this.condition, value);
    }

    [JsonPropertyName("p")]
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
