namespace ConfigCat.Client.Evaluation;

internal interface IRolloutEvaluator
{
    EvaluationDetails Evaluate(Setting setting, string key, string logDefaultValue, User user,
        ProjectConfig remoteConfig, EvaluationDetailsFactory detailsFactory);

    EvaluationDetails EvaluateVariationId(Setting setting, string key, string logDefaultVariationId, User user,
        ProjectConfig remoteConfig, EvaluationDetailsFactory detailsFactory);
}
