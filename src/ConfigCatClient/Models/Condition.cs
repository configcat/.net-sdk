namespace ConfigCat.Client;

/// <summary>
/// Base interface for conditions.
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
