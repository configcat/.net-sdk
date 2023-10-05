namespace ConfigCat.Client;

/// <summary>
/// Represents a condition.
/// Can be one of the following types: <see cref="IUserCondition"/>, <see cref="ISegmentCondition"/> or <see cref="IPrerequisiteFlagCondition"/>.
/// </summary>
public interface ICondition { }

internal interface IConditionProvider
{
    Condition? GetCondition(bool throwIfInvalid = true);
}

internal abstract class Condition : ICondition, IConditionProvider
{
    public Condition? GetCondition(bool throwIfInvalid = true) => this;
}
