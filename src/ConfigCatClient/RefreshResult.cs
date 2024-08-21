using System;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

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

    internal static RefreshResult Failure(RefreshErrorCode errorCode, LazyString errorMessage, Exception? errorException = null)
    {
        return new RefreshResult(errorCode, errorMessage, errorException);
    }

    internal static RefreshResult From(in FetchResult fetchResult)
    {
        return !fetchResult.IsFailure
            ? Success()
            : new RefreshResult(fetchResult.ErrorCode, fetchResult.ErrorMessage, fetchResult.ErrorException);
    }

    private readonly object? errorMessage; // either null or a string or a boxed LazyString

    private RefreshResult(RefreshErrorCode errorCode, object? errorMessage, Exception? errorException)
    {
        ErrorCode = errorCode;
        this.errorMessage = errorMessage;
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
    public string? ErrorMessage => this.errorMessage?.ToString();

    /// <summary>
    /// The <see cref="Exception"/> object related to the error in case the operation failed (if any).
    /// </summary>
    public Exception? ErrorException { get; }
}
