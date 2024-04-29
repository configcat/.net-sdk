using System;
using System.Diagnostics.CodeAnalysis;

namespace ConfigCat.Client;

/// <summary>
/// Contains the result of an <see cref="IConfigCatClient.ForceRefresh"/> or <see cref="IConfigCatClient.ForceRefreshAsync"/> operation.
/// </summary>
public readonly struct RefreshResult
{
    /// <summary>
    /// Creates an instance of the <see cref="RefreshResult"/> struct which indicates that the operation was successful.
    /// </summary>
    /// <returns>The new <see cref="RefreshResult"/> instance.</returns>
    public static RefreshResult Success()
    {
        return default;
    }

    /// <summary>
    /// Creates an instance of the <see cref="RefreshResult"/> struct which indicates that the operation failed.
    /// </summary>
    /// <returns>The new <see cref="RefreshResult"/> instance.</returns>
    public static RefreshResult Failure(RefreshErrorCode errorCode, string errorMessage, Exception? errorException = null)
    {
        return new RefreshResult(
            errorCode != RefreshErrorCode.None ? errorCode : throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null),
            errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
            errorException);
    }

    internal static RefreshResult From(in FetchResult fetchResult)
    {
        return !fetchResult.IsFailure
            ? Success()
            : Failure(fetchResult.ErrorCode, fetchResult.ErrorMessage, fetchResult.ErrorException);
    }

    private RefreshResult(RefreshErrorCode errorCode, string? errorMessage, Exception? errorException)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorException = errorException;
    }

    /// <summary>
    /// Indicates whether the operation was successful or not.
    /// </summary>
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    public bool IsSuccess => ErrorMessage is null;

    /// <summary>
    /// The code identifying the reason for the error in case the operation failed.
    /// </summary>
    public RefreshErrorCode ErrorCode { get; }

    /// <summary>
    /// Error message in case the operation failed, otherwise <see langword="null" />.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// The <see cref="Exception"/> object related to the error in case the operation failed (if any).
    /// </summary>
    public Exception? ErrorException { get; }
}
