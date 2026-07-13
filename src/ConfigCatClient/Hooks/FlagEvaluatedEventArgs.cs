using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.FlagEvaluated"/> event.
/// </summary>
public class FlagEvaluatedEventArgs : EventArgs
{
    internal FlagEvaluatedEventArgs(in EvaluationDetails evaluationDetails)
    {
        this.evaluationDetails = evaluationDetails;
    }

    private readonly EvaluationDetails evaluationDetails;
    /// <summary>
    /// The <see cref="Client.EvaluationDetails"/> object resulted from the evaluation of a feature flag or setting.
    /// </summary>
    public ref readonly EvaluationDetails EvaluationDetails => ref this.evaluationDetails;
}
