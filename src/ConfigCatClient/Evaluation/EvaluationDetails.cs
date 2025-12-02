using System;
using System.Runtime.CompilerServices;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Defines the public interface of the <see cref="EvaluationDetails"/> and <see cref="EvaluationDetails{TValue}"/> structs.
/// </summary>
public interface IEvaluationDetails
{
    /// <summary>
    /// Key of the feature flag or setting.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Evaluated value of the feature flag or setting.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Variation ID of the feature flag or setting (if available).
    /// </summary>
    string? VariationId { get; }

    /// <summary>
    /// Time of last successful config download (or <see cref="DateTime.MinValue"/> if there has been no successful download yet).
    /// </summary>
    DateTime FetchTime { get; }

    /// <summary>
    /// The User Object used for the evaluation (if available).
    /// </summary>
    User? User { get; }

    /// <summary>
    /// Indicates whether the default value passed to the setting evaluation methods like <see cref="IConfigCatClient.GetValueAsync"/>, <see cref="IConfigCatClient.GetValueDetailsAsync"/>, etc.
    /// is used as the result of the evaluation.
    /// </summary>
    bool IsDefaultValue { get; }

    /// <summary>
    /// The code identifying the reason for the error in case evaluation failed.
    /// </summary>
    EvaluationErrorCode ErrorCode { get; }

    /// <summary>
    /// Error message in case evaluation failed.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// The <see cref="Exception"/> object related to the error in case evaluation failed (if any).
    /// </summary>
    Exception? ErrorException { get; }

    /// <summary>
    /// The targeting rule (if any) that matched during the evaluation and was used to return the evaluated value.
    /// </summary>
    TargetingRule? MatchedTargetingRule { get; }

    /// <summary>
    /// The percentage option (if any) that was used to select the evaluated value.
    /// </summary>
    PercentageOption? MatchedPercentageOption { get; }
}

/// <summary>
/// The evaluated value and additional information about the evaluation of a feature flag or setting.
/// </summary>
public readonly struct EvaluationDetails : IEvaluationDetails
{
    /// <summary>
    /// Creates an instance of the <see cref="EvaluationDetails{TValue}"/> struct which indicates that the evaluation was successful.
    /// </summary>
    /// <returns>The new <see cref="EvaluationDetails{TValue}"/> instance.</returns>
    public static EvaluationDetails<TValue> Success<TValue>(string key, TValue value, string? variationId = null,
        TargetingRule? matchedTargetingRule = null, PercentageOption? matchedPercentageOption = null, User? user = null, DateTime? fetchTime = null)
    {
        return new EvaluationDetails<TValue>(
            key ?? throw new ArgumentNullException(nameof(key)),
            value,
            variationId,
            fetchTime ?? DateTime.MinValue,
            user,
            matchedTargetingRule: matchedTargetingRule,
            matchedPercentageOption: matchedPercentageOption);
    }

    /// <summary>
    /// Creates an instance of the <see cref="EvaluationDetails{TValue}"/> struct which indicates that the evaluation failed.
    /// </summary>
    /// <returns>The new <see cref="EvaluationDetails{TValue}"/> instance.</returns>
    public static EvaluationDetails<TValue> Failure<TValue>(string key, TValue defaultValue,
        EvaluationErrorCode errorCode, string errorMessage, Exception? errorException = null, User? user = null, DateTime? fetchTime = null)
    {
        return new EvaluationDetails<TValue>(
            key ?? throw new ArgumentNullException(nameof(key)),
            defaultValue,
            variationId: null,
            fetchTime ?? DateTime.MinValue,
            user,
            isDefaultValue: true,
            errorCode != EvaluationErrorCode.None ? errorCode : throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null),
            errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
            errorException);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static EvaluationDetails<TValue> FromEvaluateResult<TValue>(string key, TValue value, in EvaluateResult evaluateResult,
        DateTime? fetchTime, User? user)
    {
        return new EvaluationDetails<TValue>(
            key,
            value,
            evaluateResult.VariationId,
            fetchTime ?? DateTime.MinValue,
            user,
            matchedTargetingRule: evaluateResult.MatchedTargetingRule,
            matchedPercentageOption: evaluateResult.MatchedPercentageOption);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static EvaluationDetails<TValue> FromDefaultValue<TValue>(string key, TValue defaultValue, DateTime? fetchTime, User? user,
        LazyString errorMessage, Exception? errorException = null, EvaluationErrorCode errorCode = EvaluationErrorCode.UnexpectedError)
    {
        return new EvaluationDetails<TValue>(
            key,
            defaultValue,
            variationId: null,
            fetchTime ?? DateTime.MinValue,
            user,
            isDefaultValue: true,
            errorCode,
            errorMessage.IsValueCreated ? errorMessage.Value : (object)errorMessage,
            errorException);
    }

    internal readonly string key;
    internal readonly object? errorMessage; // either null or a string or a boxed LazyString

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EvaluationDetails(
        string key,
        object? value,
        string? variationId,
        DateTime fetchTime,
        User? user,
        bool isDefaultValue,
        EvaluationErrorCode errorCode,
        object? errorMessage,
        Exception? errorException,
        TargetingRule? matchedTargetingRule,
        PercentageOption? matchedPercentageOption)
    {
        this.key = key;
        Value = value;
        VariationId = variationId;
        FetchTime = fetchTime;
        User = user;
        IsDefaultValue = isDefaultValue;
        ErrorCode = errorCode;
        this.errorMessage = errorMessage;
        ErrorException = errorException;
        MatchedTargetingRule = matchedTargetingRule;
        MatchedPercentageOption = matchedPercentageOption;
    }

    /// <inheritdoc/>
    public string Key => this.key ?? string.Empty;

    /// <inheritdoc/>
    public object? Value { get; }

    /// <inheritdoc/>
    public string? VariationId { get; }

    /// <inheritdoc/>
    public DateTime FetchTime { get; }

    /// <inheritdoc/>
    public User? User { get; }

    /// <inheritdoc/>
    public bool IsDefaultValue { get; }

    /// <inheritdoc/>
    public EvaluationErrorCode ErrorCode { get; }

    /// <inheritdoc/>
    public string? ErrorMessage => this.errorMessage?.ToString();

    /// <inheritdoc/>
    public Exception? ErrorException { get; }

    /// <inheritdoc/>
    public TargetingRule? MatchedTargetingRule { get; }

    /// <inheritdoc/>
    public PercentageOption? MatchedPercentageOption { get; }
}

/// <summary>
/// The evaluated value and additional information about the evaluation of a feature flag or setting.
/// </summary>
public readonly struct EvaluationDetails<TValue> : IEvaluationDetails
{
    internal readonly string key;
    internal readonly object? errorMessage; // either null or a string or a boxed LazyString

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EvaluationDetails(
        string key,
        TValue value,
        string? variationId,
        DateTime fetchTime,
        User? user,
        bool isDefaultValue = false,
        EvaluationErrorCode errorCode = EvaluationErrorCode.None,
        object? errorMessage = null,
        Exception? errorException = null,
        TargetingRule? matchedTargetingRule = null,
        PercentageOption? matchedPercentageOption = null)
    {
        this.key = key;
        Value = value;
        VariationId = variationId;
        FetchTime = fetchTime;
        User = user;
        IsDefaultValue = isDefaultValue;
        ErrorCode = errorCode;
        this.errorMessage = errorMessage;
        ErrorException = errorException;
        MatchedTargetingRule = matchedTargetingRule;
        MatchedPercentageOption = matchedPercentageOption;
    }

    /// <inheritdoc/>
    public string Key => this.key ?? string.Empty;

    /// <summary>
    /// Evaluated value of the feature flag or setting.
    /// </summary>
    public TValue Value { get; }

    /// <inheritdoc/>
    readonly object? IEvaluationDetails.Value => Value;

    /// <inheritdoc/>
    public string? VariationId { get; }

    /// <inheritdoc/>
    public DateTime FetchTime { get; }

    /// <inheritdoc/>
    public User? User { get; }

    /// <inheritdoc/>
    public bool IsDefaultValue { get; }

    /// <inheritdoc/>
    public EvaluationErrorCode ErrorCode { get; }

    /// <inheritdoc/>
    public string? ErrorMessage => this.errorMessage?.ToString();

    /// <inheritdoc/>
    public Exception? ErrorException { get; }

    /// <inheritdoc/>
    public TargetingRule? MatchedTargetingRule { get; }

    /// <inheritdoc/>
    public PercentageOption? MatchedPercentageOption { get; }

    /// <summary>
    /// Converts a typed instance to an untyped one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator EvaluationDetails(EvaluationDetails<TValue> evaluationDetails)
    {
        return new EvaluationDetails(
            evaluationDetails.key,
            evaluationDetails.Value,
            evaluationDetails.VariationId,
            evaluationDetails.FetchTime,
            evaluationDetails.User,
            evaluationDetails.IsDefaultValue,
            evaluationDetails.ErrorCode,
            evaluationDetails.errorMessage,
            evaluationDetails.ErrorException,
            evaluationDetails.MatchedTargetingRule,
            evaluationDetails.MatchedPercentageOption);
    }

    /// <summary>
    /// Converts an untyped instance to a typed one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator EvaluationDetails<TValue>(EvaluationDetails evaluationDetails)
    {
        return new EvaluationDetails<TValue>(
            evaluationDetails.key,
            (TValue)evaluationDetails.Value!,
            evaluationDetails.VariationId,
            evaluationDetails.FetchTime,
            evaluationDetails.User,
            evaluationDetails.IsDefaultValue,
            evaluationDetails.ErrorCode,
            evaluationDetails.errorMessage,
            evaluationDetails.ErrorException,
            evaluationDetails.MatchedTargetingRule,
            evaluationDetails.MatchedPercentageOption);
    }
}
