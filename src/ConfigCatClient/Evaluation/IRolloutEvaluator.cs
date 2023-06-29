namespace ConfigCat.Client.Evaluation;

internal interface IRolloutEvaluator
{
    EvaluateResult Evaluate(ref EvaluateContext context);
}
