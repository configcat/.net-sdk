using System;
using System.Diagnostics;

#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client;

internal delegate EvaluationDetails EvaluationDetailsFactory(SettingType settingType, JsonValue value);

/// <summary>
/// The evaluated value and additional information about the evaluation of a feature flag or setting.
/// </summary>
public abstract record class EvaluationDetails
{
    private static EvaluationDetails<TValue> Create<TValue>(JsonValue value)
    {
        return new EvaluationDetails<TValue> { Value = value.ConvertTo<TValue>() };
    }

    internal static EvaluationDetails<TValue> Create<TValue>(SettingType settingType, JsonValue value)
    {
        // NOTE: We've already checked earlier in the call chain that TValue is an allowed type (see also TypeExtensions.EnsureSupportedSettingClrType).
        Debug.Assert(typeof(TValue) == typeof(object) || typeof(TValue).ToSettingType() != SettingType.Unknown, "Type is not supported.");

        // SettingType was not specified in the config JSON?
        if (settingType == SettingType.Unknown)
        {
            // Let's try to infer it from the JSON value.
            settingType = value.DetermineSettingType();

            if (settingType == SettingType.Unknown)
            {
                throw new ArgumentException($"The type of setting value '{value}' is not supported.", nameof(value));
            }
        }

        if (typeof(TValue) != typeof(object))
        {
            if (settingType != typeof(TValue).ToSettingType())
            {
                throw new InvalidOperationException($"The type of a setting must match the type of the setting's default value.{Environment.NewLine}Setting's type was {settingType} but the default value's type was {typeof(TValue)}.{Environment.NewLine}Please use a default value which corresponds to the setting type {settingType}.");
            }

            return Create<TValue>(value);
        }
        else
        {
            EvaluationDetails evaluationDetails = new EvaluationDetails<object> { Value = value.ConvertToObject(settingType) };
            return (EvaluationDetails<TValue>)evaluationDetails;
        }
    }

    internal static EvaluationDetails Create(SettingType settingType, JsonValue value)
    {
        return settingType switch
        {
            SettingType.Boolean => Create<bool>(value),
            SettingType.String => Create<string>(value),
            SettingType.Int => Create<int>(value),
            SettingType.Double => Create<double>(value),
            _ => throw new ArgumentOutOfRangeException(nameof(settingType), settingType, null)
        };
    }

    internal static EvaluationDetails FromJsonValue(
        EvaluationDetailsFactory factory,
        SettingType settingType,
        string key,
        JsonValue value,
        string? variationId,
        DateTime? fetchTime,
        User? user,
        RolloutRule? matchedEvaluationRule = null,
        RolloutPercentageItem? matchedEvaluationPercentageRule = null)
    {
        var instance = factory(settingType, value);

        instance.Key = key;
        instance.VariationId = variationId;
        if (fetchTime is not null)
        {
            instance.FetchTime = fetchTime.Value;
        }
        instance.User = user;
        instance.MatchedEvaluationRule = matchedEvaluationRule;
        instance.MatchedEvaluationPercentageRule = matchedEvaluationPercentageRule;

        return instance;
    }

    internal static EvaluationDetails<TValue> FromDefaultValue<TValue>(string key, TValue defaultValue, DateTime? fetchTime, User? user,
        string? errorMessage = null, Exception? errorException = null)
    {
        var instance = new EvaluationDetails<TValue>
        {
            Key = key,
            Value = defaultValue,
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
    public ITargetingRule? MatchedEvaluationRule { get; set; }

    /// <summary>
    /// The percentage option which was used to select the evaluated value (if any).
    /// </summary>
    public IPercentageOption? MatchedEvaluationPercentageRule { get; set; }
}

/// <inheritdoc/>
public sealed record class EvaluationDetails<TValue> : EvaluationDetails
{
    internal EvaluationDetails() : this(key: null!, value: default!) { }

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
