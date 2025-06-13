using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal static class EvaluationHelper
{
    public static EvaluationDetails<T> Evaluate<T>(this IRolloutEvaluator evaluator, Dictionary<string, Setting>? settings, string key, T defaultValue, User? user,
        ProjectConfig? remoteConfig, LoggerWrapper logger)
    {
        FormattableLogMessage logMessage;

        if (settings is null)
        {
            logMessage = logger.ConfigJsonIsNotPresent(key, nameof(defaultValue), defaultValue);
            return EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user,
                logMessage.ToLazyString(), errorCode: EvaluationErrorCode.ConfigJsonNotAvailable);
        }

        if (!settings.TryGetValue(key, out var setting))
        {
            var availableKeys = new StringListFormatter(settings.Keys);
            logMessage = logger.SettingEvaluationFailedDueToMissingKey(key, nameof(defaultValue), defaultValue, availableKeys);
            return EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user,
                logMessage.ToLazyString(), errorCode: EvaluationErrorCode.SettingKeyMissing);
        }

        var evaluateContext = new EvaluateContext(key, setting, user, settings);
        // NOTE: It's better to avoid virtual generic method calls as they are slow and may be problematic for older AOT compilers (like Mono AOT or IL2CPP),
        // especially, when targeting platforms which disallow the execution of dynamically generated code (e.g. Xamarin.iOS).
        var evaluateResult = evaluator is RolloutEvaluator rolloutEvaluator
            ? rolloutEvaluator.Evaluate(defaultValue, ref evaluateContext, out var value)
            : evaluator.Evaluate(defaultValue, ref evaluateContext, out value);
        return EvaluationDetails.FromEvaluateResult(key, value, evaluateResult, fetchTime: remoteConfig?.TimeStamp, user);
    }

    public static EvaluationDetails[] EvaluateAll(this IRolloutEvaluator evaluator, Dictionary<string, Setting>? settings, User? user,
        ProjectConfig? remoteConfig, LoggerWrapper logger, string defaultReturnValue, out IReadOnlyList<Exception>? exceptions)
    {
        if (!CheckSettingsAvailable(settings, logger, defaultReturnValue))
        {
            exceptions = null;
            return Array.Empty<EvaluationDetails>();
        }

        var evaluationDetailsArray = new EvaluationDetails[settings.Count];
        List<Exception>? exceptionList = null;
        var rolloutEvaluator = evaluator as RolloutEvaluator;

        var index = 0;
        foreach (var kvp in settings)
        {
            EvaluationDetails evaluationDetails;
            try
            {
                var evaluateContext = new EvaluateContext(kvp.Key, kvp.Value, user, settings);
                // NOTE: It's better to avoid virtual generic method calls as they are slow and may be problematic for older AOT compilers (like Mono AOT or IL2CPP),
                // especially, when targeting platforms which disallow the execution of dynamically generated code (e.g. Xamarin.iOS).
                var evaluateResult = rolloutEvaluator is not null
                    ? rolloutEvaluator.Evaluate<object?>(defaultValue: null, ref evaluateContext, out var value)
                    : evaluator.Evaluate(defaultValue: null, ref evaluateContext, out value);
                evaluationDetails = EvaluationDetails.FromEvaluateResult(kvp.Key, value, evaluateResult, fetchTime: remoteConfig?.TimeStamp, user);
            }
            catch (Exception ex)
            {
                exceptionList ??= new List<Exception>();
                exceptionList.Add(ex);
                evaluationDetails = EvaluationDetails.FromDefaultValue<object?>(kvp.Key, defaultValue: null, fetchTime: remoteConfig?.TimeStamp, user,
                    ex.Message, ex, GetErrorCode(ex));
            }

            evaluationDetailsArray[index++] = evaluationDetails;
        }

        exceptions = exceptionList;
        return evaluationDetailsArray;
    }

    internal static KeyValuePair<string, T>? GetKeyAndValue<T>(Dictionary<string, Setting>? settings, string variationId, LoggerWrapper logger, string defaultReturnValue)
    {
        if (!CheckSettingsAvailable(settings, logger, defaultReturnValue))
        {
            return null;
        }

        if (FindKeyAndValue(settings, variationId, out var settingType) is { } kvp)
        {
            T value;

            if (typeof(T) != typeof(object))
            {
                var expectedSettingType = typeof(T).ToSettingType();

                // NOTE: We've already checked earlier in the call chain that T is an allowed type (see also TypeExtensions.EnsureSupportedSettingClrType).
                Debug.Assert(expectedSettingType != Setting.UnknownType, "Type is not supported.");

                value = kvp.Value.GetValue<T>(expectedSettingType)!;
            }
            else
            {
                value = (T)(settingType != Setting.UnknownType
                    ? kvp.Value.GetValue(settingType)!
                    : kvp.Value.GetValue()!);
            }

            return new KeyValuePair<string, T>(kvp.Key, value);
        }

        logger.SettingForVariationIdIsNotPresent(variationId);
        return null;
    }

    private static KeyValuePair<string, SettingValue>? FindKeyAndValue(Dictionary<string, Setting> settings, string variationId, out SettingType settingType)
    {
        foreach (var kvp in settings)
        {
            var key = kvp.Key;
            var setting = kvp.Value;

            if (setting.variationId == variationId)
            {
                settingType = setting.settingType;
                return new KeyValuePair<string, SettingValue>(key, setting.value);
            }

            foreach (var targetingRule in setting.TargetingRulesOrEmpty)
            {
                if (targetingRule.SimpleValueOrNull is { } simpleValue)
                {
                    if (simpleValue.variationId == variationId)
                    {
                        settingType = setting.settingType;
                        return new KeyValuePair<string, SettingValue>(key, simpleValue.value);
                    }
                }
                else if (targetingRule.PercentageOptionsOrNull is { Length: > 0 } percentageOptions)
                {
                    foreach (var percentageOption in percentageOptions)
                    {
                        if (percentageOption.variationId == variationId)
                        {
                            settingType = setting.settingType;
                            return new KeyValuePair<string, SettingValue>(key, percentageOption.value);
                        }
                    }
                }
                else
                {
                    throw new InvalidConfigModelException("Targeting rule THEN part is missing or invalid.");
                }
            }

            foreach (var percentageOption in setting.PercentageOptionsOrEmpty)
            {
                if (percentageOption.variationId == variationId)
                {
                    settingType = setting.settingType;
                    return new KeyValuePair<string, SettingValue>(key, percentageOption.value);
                }
            }
        }

        settingType = Setting.UnknownType;
        return null;
    }

    internal static bool CheckSettingsAvailable([NotNullWhen(true)] Dictionary<string, Setting>? settings, LoggerWrapper logger, string defaultReturnValue)
    {
        if (settings is null)
        {
            logger.ConfigJsonIsNotPresent(defaultReturnValue);
            return false;
        }

        return true;
    }

    internal static EvaluationErrorCode GetErrorCode(Exception exception)
    {
        return exception switch
        {
            EvaluationErrorException evaluationErrorException => evaluationErrorException.ErrorCode,
            InvalidConfigModelException => EvaluationErrorCode.InvalidConfigModel,
            _ => EvaluationErrorCode.UnexpectedError,
        };
    }
}
