using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal static class RolloutEvaluatorExtensions
{
    public static EvaluationDetails<T> Evaluate<T>(this IRolloutEvaluator evaluator, Dictionary<string, Setting>? settings, string key, T defaultValue, User? user,
        ProjectConfig? remoteConfig, LoggerWrapper logger)
    {
        FormattableLogMessage logMessage;

        if (settings is null)
        {
            logMessage = logger.ConfigJsonIsNotPresent(key, nameof(defaultValue), defaultValue);
            return EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user, logMessage.InvariantFormattedMessage);
        }

        if (!settings.TryGetValue(key, out var setting))
        {
            var availableKeys = new StringListFormatter(settings.Keys).ToString();
            logMessage = logger.SettingEvaluationFailedDueToMissingKey(key, nameof(defaultValue), defaultValue, availableKeys);
            return EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user, logMessage.InvariantFormattedMessage);
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
            return ArrayUtils.EmptyArray<EvaluationDetails>();
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
                evaluationDetails = EvaluationDetails.FromDefaultValue<object?>(kvp.Key, defaultValue: null, fetchTime: remoteConfig?.TimeStamp, user, ex.Message, ex);
            }

            evaluationDetailsArray[index++] = evaluationDetails;
        }

        exceptions = exceptionList;
        return evaluationDetailsArray;
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
}
