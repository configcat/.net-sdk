using System.Collections.Generic;

namespace ConfigCat.Client.Evaluation
{
    internal static class RolloutEvaluatorExtensions
    {
        public static T Evaluate<T>(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, T defaultValue, User user = null, ProjectConfig remoteConfig = null)
        {
            var result = evaluator.Evaluate(settings, key, defaultValue?.ToString(), user, remoteConfig, static (_, value) => EvaluationDetails.Create<T>(value));
            return result is not null ? ((EvaluationDetails<T>)result).Value : defaultValue;
        }

        public static object Evaluate(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, object defaultValue, User user = null, ProjectConfig remoteConfig = null)
        {
            var result = evaluator.Evaluate(settings, key, defaultValue?.ToString(), user, remoteConfig, static (settingType, value) => EvaluationDetails.Create(settingType, value));
            return result is not null ? result.Value : defaultValue;
        }

        public static EvaluationDetails<T> EvaluateDetails<T>(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, T defaultValue, User user = null, ProjectConfig remoteConfig = null)
        {
            var result = evaluator.Evaluate(settings, key, defaultValue?.ToString(), user, remoteConfig, static (_, value) => EvaluationDetails.Create<T>(value));
            return result is not null ? (EvaluationDetails<T>)result : EvaluationDetails.FromDefaultValue(key, defaultValue, fetchTime: remoteConfig?.TimeStamp, user);
        }

        public static string EvaluateVariationId(this IRolloutEvaluator evaluator, IDictionary<string, Setting> settings, string key, string defaultVariationId, User user = null, ProjectConfig remoteConfig = null)
        {
            var result = evaluator.EvaluateVariationIdWithDetails(settings, key, defaultVariationId, user, remoteConfig);
            return result is not null ? result.VariationId : defaultVariationId;
        }
    }
}