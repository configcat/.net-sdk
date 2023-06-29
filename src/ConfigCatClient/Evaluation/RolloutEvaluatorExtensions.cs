using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal static class RolloutEvaluatorExtensions
{
    public static EvaluationDetails<T> Evaluate<T>(this IRolloutEvaluator evaluator, Setting setting, string key, T defaultValue, User? user,
        ProjectConfig? remoteConfig)
    {
        var evaluateContext = new EvaluateContext(key, setting, defaultValue.ToSettingValue(out _), user);
        var evaluateResult = evaluator.Evaluate(ref evaluateContext);
        return EvaluationDetails.FromEvaluateResult<T>(key, evaluateResult, setting.SettingType, fetchTime: remoteConfig?.TimeStamp, user);
    }

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

        return evaluator.Evaluate(setting, key, defaultValue, user, remoteConfig);
    }

    public static EvaluationDetails Evaluate(this IRolloutEvaluator evaluator, Setting setting, string key, User? user, ProjectConfig? remoteConfig)
    {
        var evaluateContext = new EvaluateContext(key, setting, default, user);
        var evaluateResult = evaluator.Evaluate(ref evaluateContext);
        return EvaluationDetails.FromEvaluateResult<object>(key, evaluateResult, setting.SettingType, fetchTime: remoteConfig?.TimeStamp, user);
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

        var index = 0;
        foreach (var kvp in settings)
        {
            EvaluationDetails evaluationDetails;
            try
            {
                evaluationDetails = evaluator.Evaluate(kvp.Value, kvp.Key, user, remoteConfig);
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
