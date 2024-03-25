using System;

namespace ConfigCat.Client.Evaluation;

internal sealed class EvaluationErrorException : InvalidOperationException
{
    public EvaluationErrorException(EvaluationErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public EvaluationErrorCode ErrorCode { get; }
}
