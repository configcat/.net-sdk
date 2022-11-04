using System;
using System.Collections.Generic;
using System.Linq;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation
{
    internal static class RolloutEvaluatorExtensions
    {
        public static T Evaluate<T>(this IRolloutEvaluator evaluator, Setting setting, string key, T defaultValue, User user,
            ProjectConfig remoteConfig, out EvaluationDetails<T> evaluationDetails)
        {
            evaluationDetails = (EvaluationDetails<T>)evaluator.Evaluate(setting, key, defaultValue?.ToString(), user, remoteConfig, static (_, value) => EvaluationDetails.Create<T>(value));
            return evaluationDetails.Value;
        }

        public static T Evaluate<T>(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, T defaultValue, User user,
            ProjectConfig remoteConfig, ILogger logger, out EvaluationDetails<T> evaluationDetails)
        {
            string errorMessage;

            if (settings.Count == 0)
            {
                errorMessage = $"Config JSON is not present. Returning the {nameof(defaultValue)} that you specified in the source code: '{defaultValue}'.";
                logger.Error(errorMessage);
                evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
                return defaultValue;
            }

            if (!settings.TryGetValue(key, out var setting))
            {
                errorMessage = $"Evaluating '{key}' failed (key was not found in config JSON). Returning the {nameof(defaultValue)} that you specified in the source code: '{defaultValue}'. These are the available keys: {KeysToString(settings)}.";
                logger.Error(errorMessage);
                evaluationDetails = EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
                return defaultValue;
            }

            return evaluator.Evaluate(setting, key, defaultValue, user, remoteConfig, out evaluationDetails);
        }

        public static object Evaluate(this IRolloutEvaluator evaluator, Setting setting, string key, object defaultValue, User user,
            ProjectConfig remoteConfig, out EvaluationDetails evaluationDetails)
        {
            evaluationDetails = evaluator.Evaluate(setting, key, defaultValue?.ToString(), user, remoteConfig, static (settingType, value) => EvaluationDetails.Create(settingType, value));
            return evaluationDetails.Value;
        }

        public static IDictionary<string, object> EvaluateAll(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, User user,
            ProjectConfig remoteConfig, ILogger logger, out EvaluationDetails[] evaluationDetailsArray)
        {
            if (!CheckSettingsAvailable(settings, logger))
            {
                evaluationDetailsArray = ArrayUtils.EmptyArray<EvaluationDetails>();
                return new Dictionary<string, object>();
            }

            var result = new Dictionary<string, object>(settings.Count);
            evaluationDetailsArray = new EvaluationDetails[settings.Count];
            List<Exception> exceptions = null;

            int index = 0;
            foreach (var kvp in settings)
            {
                object value;
                EvaluationDetails evaluationDetails;
                try
                {
                    value = evaluator.Evaluate(kvp.Value, kvp.Key, defaultValue: null, user, remoteConfig, out evaluationDetails);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                    evaluationDetails = EvaluationDetails.FromDefaultValue(kvp.Key, defaultValue: (object)null, fetchTime: remoteConfig?.TimeStamp, user, ex.Message, ex);
                    value = null;
                }

                evaluationDetailsArray[index++] = evaluationDetails;
                result.Add(kvp.Key, value);
            }

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }

            return result;
        }

        public static string EvaluateVariationId(this IRolloutEvaluator evaluator, Setting setting, string key, string defaultVariationId, User user,
            ProjectConfig remoteConfig, out EvaluationDetails evaluationDetails)
        {
            evaluationDetails = evaluator.EvaluateVariationId(setting, key, defaultVariationId, user, remoteConfig, static (settingType, value) => EvaluationDetails.Create(settingType, value));
            return evaluationDetails.VariationId;
        }

        public static string EvaluateVariationId(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, string defaultVariationId, User user,
            ProjectConfig remoteConfig, ILogger logger, out EvaluationDetails evaluationDetails)
        {
            string errorMessage;

            if (settings.Count == 0)
            {
                errorMessage = $"Config JSON is not present. Returning the {nameof(defaultVariationId)} defined in the app source code: '{defaultVariationId}'.";
                logger.Error(errorMessage);
                evaluationDetails = EvaluationDetails.FromDefaultVariationId(key, defaultVariationId, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
                return defaultVariationId;
            }

            if (!settings.TryGetValue(key, out var setting))
            {
                errorMessage = $"Evaluating '{key}' failed (key was not found in config JSON). Returning the {nameof(defaultVariationId)} that you specified in the source code: '{defaultVariationId}'. These are the available keys: {KeysToString(settings)}.";
                logger.Error(errorMessage);
                evaluationDetails = EvaluationDetails.FromDefaultVariationId(key, defaultVariationId, fetchTime: remoteConfig?.TimeStamp, user, errorMessage);
                return defaultVariationId;
            }

            return evaluator.EvaluateVariationId(setting, key, defaultVariationId, user, remoteConfig, out evaluationDetails);
        }

        public static IList<string> EvaluateAllVariationIds(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, User user,
            ProjectConfig remoteConfig, ILogger logger, out EvaluationDetails[] evaluationDetailsArray)
        {
            if (!CheckSettingsAvailable(settings, logger))
            {
                evaluationDetailsArray = ArrayUtils.EmptyArray<EvaluationDetails>();
                return ArrayUtils.EmptyArray<string>();
            }

            var result = new List<string>(settings.Count);
            evaluationDetailsArray = new EvaluationDetails[settings.Count];
            List<Exception> exceptions = null;

            int index = 0;
            foreach (var kvp in settings)
            {
                string variationId;
                EvaluationDetails evaluationDetails;
                try
                {
                    variationId = evaluator.EvaluateVariationId(kvp.Value, kvp.Key, defaultVariationId: null, user, remoteConfig, out evaluationDetails);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                    evaluationDetails = EvaluationDetails.FromDefaultVariationId(kvp.Key, defaultVariationId: null, fetchTime: remoteConfig?.TimeStamp, user, ex.Message, ex);
                    variationId = null;
                }

                evaluationDetailsArray[index++] = evaluationDetails;
                if (variationId is not null)
                {
                    result.Add(variationId);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException(exceptions);
            }

            result.TrimExcess();
            return result;
        }

        internal static bool CheckSettingsAvailable(IDictionary<string, Setting> settings, ILogger logger)
        {
            if (settings.Count == 0)
            {
                logger.Warning("Config JSON is not present.");
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