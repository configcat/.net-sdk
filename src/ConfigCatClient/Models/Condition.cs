namespace ConfigCat.Client;

internal interface IConditionProvider
{
    Condition? GetCondition(bool throwIfInvalid = true);
}

/// <summary>
/// Describes a condition.
/// Can be one of the following types: <see cref="UserCondition"/>, <see cref="SegmentCondition"/> or <see cref="PrerequisiteFlagCondition"/>.
/// </summary>
public abstract class Condition : IConditionProvider
{
    private protected Condition() { }

    Condition? IConditionProvider.GetCondition(bool throwIfInvalid) => this;
}
