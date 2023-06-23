namespace ConfigCat.Client.Evaluation;

internal interface IRolloutEvaluator
{
    EvaluateResult Evaluate(Setting setting, string key, string? logDefaultValue, User? user);
}
