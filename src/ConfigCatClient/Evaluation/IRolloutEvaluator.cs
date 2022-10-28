using System.Collections.Generic;

namespace ConfigCat.Client.Evaluation
{
    internal interface IRolloutEvaluator
    {
        EvaluationDetails Evaluate(IDictionary<string, Setting> settings, string key, string logDefaultValue, User user, ProjectConfig remoteConfig, EvaluationDetailsFactory detailsFactory);
        EvaluationDetails EvaluateVariationIdWithDetails(IDictionary<string, Setting> settings, string key, string logDefaultVariationId, User user, ProjectConfig remoteConfig);
    }
}