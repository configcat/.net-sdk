using System.Diagnostics.CodeAnalysis;

namespace ConfigCat.Client.Evaluation;

internal interface IRolloutEvaluator
{
    EvaluateResult Evaluate<T>(T defaultValue, ref EvaluateContext context, [NotNull] out T returnValue);
}
