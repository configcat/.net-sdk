using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.FlagEvaluated"/> event.
/// </summary>
public class FlagEvaluatedEventArgs : EventArgs
{
    internal FlagEvaluatedEventArgs(EvaluationDetails evaluationDetails)
    {
        EvaluationDetails = evaluationDetails;
    }

    /// <summary>
    /// The <see cref="Client.EvaluationDetails"/> object resulted from the evaluation of a feature flag or setting.
    /// </summary>
    public EvaluationDetails EvaluationDetails { get; }
}
