using System;
using System.Diagnostics.CodeAnalysis;

namespace ConfigCat.Client;

internal readonly struct FetchResult
{
    private static readonly object NotModifiedToken = new();

    public static FetchResult Success(ProjectConfig config)
    {
        return new FetchResult(config, RefreshErrorCode.None, errorMessageOrToken: null);
    }

    public static FetchResult NotModified(ProjectConfig config)
    {
        return new FetchResult(config, RefreshErrorCode.None, NotModifiedToken);
    }

    public static FetchResult Failure(ProjectConfig config, RefreshErrorCode errorCode, string errorMessage, Exception? errorException = null)
    {
        return new FetchResult(config, errorCode, errorMessage, errorException);
    }

    private readonly object? errorMessageOrToken;

    private FetchResult(ProjectConfig config, RefreshErrorCode errorCode, object? errorMessageOrToken, Exception? errorException = null)
    {
        Config = config;
        this.errorMessageOrToken = errorMessageOrToken;
        ErrorCode = errorCode;
        ErrorException = errorException;
    }

    public bool IsSuccess => this.errorMessageOrToken is null;
    public bool IsNotModified => ReferenceEquals(this.errorMessageOrToken, NotModifiedToken);
    [MemberNotNullWhen(true, nameof(ErrorMessage))]
    public bool IsFailure => this.errorMessageOrToken is string;

    public ProjectConfig Config { get; }
    public RefreshErrorCode ErrorCode { get; }
    public string? ErrorMessage => this.errorMessageOrToken as string;
    public Exception? ErrorException { get; }
}
