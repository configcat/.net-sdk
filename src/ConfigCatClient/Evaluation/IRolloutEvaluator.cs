namespace ConfigCat.Client.Evaluation;

internal interface IRolloutEvaluator
{
    EvaluateResult Evaluate(in EvaluateContext context);
}
