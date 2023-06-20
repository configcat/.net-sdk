using System;
using System.Diagnostics;
using ConfigCat.Client.Evaluation;

#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client;

/// <summary>
/// The evaluated value and additional information about the evaluation of a feature flag or setting.
/// </summary>
public abstract record class EvaluationDetails
{
    private static void EnsureValidSettingValue(JsonValue value, ref SettingType settingType, string? unsupportedTypeError)
    {
        // Setting type is not known (it's not present in the config JSON, it's an unsupported value coming from a flag override, etc.)?
        if (settingType == SettingType.Unknown)
        {
            // Let's try to infer it from the JSON value.
            settingType = value.DetermineSettingType();

            if (settingType == SettingType.Unknown)
            {
                throw new ArgumentException(unsupportedTypeError ?? $"Setting value '{value}' is of an unsupported type.", nameof(value));
            }
        }
    }

    private static EvaluationDetails<TValue> Create<TValue>(string key, JsonValue value)
    {
        return new EvaluationDetails<TValue>(key, value.ConvertTo<TValue>());
    }

    internal static EvaluationDetails<TValue> FromEvaluateResult<TValue>(string key, in EvaluateResult evaluateResult, SettingType settingType, string? unsupportedTypeError,
        DateTime? fetchTime, User? user)
    {
        // NOTE: We've already checked earlier in the call chain that TValue is an allowed type (see also TypeExtensions.EnsureSupportedSettingClrType).
        Debug.Assert(typeof(TValue) == typeof(object) || typeof(TValue).ToSettingType() != SettingType.Unknown, "Type is not supported.");

        var value = evaluateResult.Value;
        EnsureValidSettingValue(value, ref settingType, unsupportedTypeError);

        EvaluationDetails<TValue> instance;

        if (typeof(TValue) != typeof(object))
        {
            if (settingType != typeof(TValue).ToSettingType())
            {
                throw new InvalidOperationException($"The type of a setting must match the type of the setting's default value.{Environment.NewLine}Setting's type was {settingType} but the default value's type was {typeof(TValue)}.{Environment.NewLine}Please use a default value which corresponds to the setting type {settingType}.");
            }

            instance = Create<TValue>(key, value);
        }
        else
        {
            EvaluationDetails evaluationDetails = new EvaluationDetails<object>(key, value.ConvertToObject(settingType));
            instance = (EvaluationDetails<TValue>)evaluationDetails;
        }

        instance.Initialize(evaluateResult, fetchTime, user);
        return instance;
    }

    internal static EvaluationDetails FromEvaluateResult(string key, in EvaluateResult evaluateResult, SettingType settingType, string? unsupportedTypeError,
        DateTime? fetchTime, User? user)
    {
        var value = evaluateResult.Value;
        EnsureValidSettingValue(value, ref settingType, unsupportedTypeError);

        EvaluationDetails instance = settingType switch
        {
            SettingType.Boolean => Create<bool>(key, value),
            SettingType.String => Create<string>(key, value),
            SettingType.Int => Create<int>(key, value),
            SettingType.Double => Create<double>(key, value),
            _ => throw new ArgumentOutOfRangeException(nameof(settingType), settingType, null)
        };

        instance.Initialize(evaluateResult, fetchTime, user);
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

    private void Initialize(in EvaluateResult evaluateResult, DateTime? fetchTime, User? user)
    {
        VariationId = evaluateResult.VariationId;
        if (fetchTime is not null)
        {
            FetchTime = fetchTime.Value;
        }
        User = user;
        MatchedEvaluationRule = evaluateResult.MatchedTargetingRule;
        MatchedEvaluationPercentageRule = evaluateResult.MatchedPercentageOption;
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
    public ITargetingRule? MatchedEvaluationRule { get; set; }

    /// <summary>
    /// The percentage option which was used to select the evaluated value (if any).
    /// </summary>
    public IPercentageOption? MatchedEvaluationPercentageRule { get; set; }
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
