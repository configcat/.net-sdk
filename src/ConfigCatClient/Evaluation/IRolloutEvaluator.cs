namespace ConfigCat.Client.Evaluation;

internal interface IRolloutEvaluator
{
    EvaluationDetails Evaluate(Setting setting, string key, string logDefaultValue, User user,
        ProjectConfig remoteConfig, EvaluationDetailsFactory detailsFactory);
}
