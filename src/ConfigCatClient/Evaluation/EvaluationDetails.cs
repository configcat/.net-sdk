using System;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

/// <summary>
/// The evaluated value and additional information about the evaluation of a feature flag or setting.
/// </summary>
public abstract record class EvaluationDetails
{
    internal static EvaluationDetails<TValue> FromEvaluateResult<TValue>(string key, TValue value, in EvaluateResult evaluateResult,
        DateTime? fetchTime, User? user)
    {
        var instance = new EvaluationDetails<TValue>(key, value)
        {
            User = user,
            VariationId = evaluateResult.VariationId,
            MatchedTargetingRule = evaluateResult.MatchedTargetingRule,
            MatchedPercentageOption = evaluateResult.MatchedPercentageOption
        };

        if (fetchTime is not null)
        {
            instance.FetchTime = fetchTime.Value;
        }

        return instance;
    }

    internal static EvaluationDetails<TValue> FromDefaultValue<TValue>(string key, TValue defaultValue, DateTime? fetchTime, User? user,
        string? errorMessage = null, Exception? errorException = null)
    {
        var instance = new EvaluationDetails<TValue>(key, defaultValue)
        {
            User = user,
            IsDefaultValue = true,
            ErrorMessage = errorMessage,
            ErrorException = errorException
        };

        if (fetchTime is not null)
        {
            instance.FetchTime = fetchTime.Value;
        }

        return instance;
    }

    private protected EvaluationDetails(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Key of the feature flag or setting.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// Evaluated value of the feature flag or setting.
    /// </summary>
    public object? Value => GetValueAsObject();

    private protected abstract object? GetValueAsObject();

    /// <summary>
    /// Variation ID of the feature flag or setting (if available).
    /// </summary>
    public string? VariationId { get; set; }

    /// <summary>
    /// Time of last successful config download (or <see cref="DateTime.MinValue"/> if there has been no successful download yet).
    /// </summary>
    public DateTime FetchTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// The User Object used for the evaluation (if available).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Indicates whether the default value passed to the setting evaluation methods like <see cref="IConfigCatClient.GetValue"/>, <see cref="IConfigCatClient.GetValueDetails"/>, etc.
    /// is used as the result of the evaluation.
    /// </summary>
    public bool IsDefaultValue { get; set; }

    /// <summary>
    /// Error message in case evaluation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The <see cref="Exception"/> object related to the error in case evaluation failed (if any).
    /// </summary>
    public Exception? ErrorException { get; set; }

    /// <summary>
    /// The targeting rule which was used to select the evaluated value (if any).
    /// </summary>
    public ITargetingRule? MatchedTargetingRule { get; set; }

    /// <summary>
    /// The percentage option which was used to select the evaluated value (if any).
    /// </summary>
    public IPercentageOption? MatchedPercentageOption { get; set; }
}

/// <inheritdoc/>
public sealed record class EvaluationDetails<TValue> : EvaluationDetails
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationDetails{TValue}"/> class.
    /// </summary>
    public EvaluationDetails(string key, TValue value) : base(key)
    {
        Value = value;
    }

    /// <inheritdoc/>
    public new TValue Value { get; set; }

    private protected override object? GetValueAsObject() => Value;
}
