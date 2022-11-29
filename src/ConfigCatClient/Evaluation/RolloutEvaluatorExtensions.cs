using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation
{
    internal static class RolloutEvaluatorExtensions
    {
        public static EvaluationDetails<T> Evaluate<T>(this IRolloutEvaluator evaluator, Setting setting, string key, T defaultValue, User user,
            ProjectConfig remoteConfig)
        {
            return (EvaluationDetails<T>)evaluator.Evaluate(setting, key, defaultValue?.ToString(), user, remoteConfig, static (settingType, value) => EvaluationDetails.Create<T>(settingType, value));
        }

        public static EvaluationDetails<T> Evaluate<T>(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, T defaultValue, User user,
            ProjectConfig remoteConfig, ILogger logger)
        {
            string errorMessage;

            if (settings is null)
            {
                errorMessage = $"Config JSON is not present. Returning the {nameof(defaultValue)} that you specified in the source code: '{defaultValue}'.";
                logger.Error(errorMessage);
                return EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
            }

            if (!settings.TryGetValue(key, out var setting))
            {
                errorMessage = $"Evaluating '{key}' failed (key was not found in config JSON). Returning the {nameof(defaultValue)} that you specified in the source code: '{defaultValue}'. These are the available keys: {KeysToString(settings)}.";
                logger.Error(errorMessage);
                return EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
            }

            return evaluator.Evaluate(setting, key, defaultValue, user, remoteConfig);
        }

        public static EvaluationDetails Evaluate(this IRolloutEvaluator evaluator, Setting setting, string key, object defaultValue, User user,
            ProjectConfig remoteConfig)
        {
            return evaluator.Evaluate(setting, key, defaultValue?.ToString(), user, remoteConfig, static (settingType, value) => EvaluationDetails.Create(settingType, value));
        }

        public static EvaluationDetails[] EvaluateAll(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, User user,
            ProjectConfig remoteConfig, ILogger logger, out IReadOnlyList<Exception> exceptions)
        {
            if (!CheckSettingsAvailable(settings, logger))
            {
                exceptions = null;
                return ArrayUtils.EmptyArray<EvaluationDetails>();
            }

            var evaluationDetailsArray = new EvaluationDetails[settings.Count];
            List<Exception> exceptionList = null;

            int index = 0;
            foreach (var kvp in settings)
            {
                EvaluationDetails evaluationDetails;
                try
                {
                    evaluationDetails = evaluator.Evaluate(kvp.Value, kvp.Key, defaultValue: null, user, remoteConfig);
                }
                catch (Exception ex)
                {
                    exceptionList ??= new List<Exception>();
                    exceptionList.Add(ex);
                    evaluationDetails = EvaluationDetails.FromDefaultValue(kvp.Key, defaultValue: (object)null, fetchTime: remoteConfig?.TimeStamp, user, ex.Message, ex);
                }

                evaluationDetailsArray[index++] = evaluationDetails;
            }

            exceptions = exceptionList;
            return evaluationDetailsArray;
        }

        public static EvaluationDetails EvaluateVariationId(this IRolloutEvaluator evaluator, Setting setting, string key, string defaultVariationId, User user,
            ProjectConfig remoteConfig)
        {
            return evaluator.EvaluateVariationId(setting, key, defaultVariationId, user, remoteConfig, static (settingType, value) => EvaluationDetails.Create(settingType, value));
        }

        public static EvaluationDetails EvaluateVariationId(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, string defaultVariationId, User user,
            ProjectConfig remoteConfig, ILogger logger)
        {
            string errorMessage;

            if (settings is null)
            {
                errorMessage = $"Config JSON is not present. Returning the {nameof(defaultVariationId)} defined in the app source code: '{defaultVariationId}'.";
                logger.Error(errorMessage);
                return EvaluationDetails.FromDefaultVariationId(key, defaultVariationId, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
            }

            if (!settings.TryGetValue(key, out var setting))
            {
                errorMessage = $"Evaluating '{key}' failed (key was not found in config JSON). Returning the {nameof(defaultVariationId)} that you specified in the source code: '{defaultVariationId}'. These are the available keys: {KeysToString(settings)}.";
                logger.Error(errorMessage);
                return EvaluationDetails.FromDefaultVariationId(key, defaultVariationId, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
            }

            return evaluator.EvaluateVariationId(setting, key, defaultVariationId, user, remoteConfig);
        }

        public static EvaluationDetails[] EvaluateAllVariationIds(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, User user,
            ProjectConfig remoteConfig, ILogger logger, out IReadOnlyList<Exception> exceptions)
        {
            if (!CheckSettingsAvailable(settings, logger))
            {
                exceptions = null;
                return ArrayUtils.EmptyArray<EvaluationDetails>();
            }

            var evaluationDetailsArray = new EvaluationDetails[settings.Count];
            List<Exception> exceptionList = null;

            int index = 0;
            foreach (var kvp in settings)
            {
                EvaluationDetails evaluationDetails;
                try
                {
                    evaluationDetails = evaluator.EvaluateVariationId(kvp.Value, kvp.Key, defaultVariationId: null, user, remoteConfig);
                }
                catch (Exception ex)
                {
                    exceptionList ??= new List<Exception>();
                    exceptionList.Add(ex);
                    evaluationDetails = EvaluationDetails.FromDefaultVariationId(kvp.Key, defaultVariationId: null, fetchTime: remoteConfig?.TimeStamp, user, ex.Message, ex);
                }

                evaluationDetailsArray[index++] = evaluationDetails;
            }

            exceptions = exceptionList;
            return evaluationDetailsArray;
        }

        internal static bool CheckSettingsAvailable(IDictionary<string, Setting> settings, ILogger logger)
        {
            if (settings is null)
            {
                logger.Error("Config JSON is not present.");
                return false;
            }

            return true;
        }

        private static string KeysToString(IDictionary<string, Setting> settings)
        {
            return string.Join(",", settings.Keys.Select(s => $"'{s}'").ToArray());
        }
    }
}