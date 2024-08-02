using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

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

    public static FetchResult Failure(ProjectConfig config, RefreshErrorCode errorCode, LazyString errorMessage, Exception? errorException = null)
    {
        Debug.Assert(!EqualityComparer<LazyString>.Default.Equals(errorMessage, default));
        return new FetchResult(config, errorCode, errorMessage.IsValueCreated ? errorMessage.Value : (object)errorMessage, errorException);
    }

    private readonly object? errorMessageOrToken; // either null or a string or a boxed LazyString or NotModifiedToken

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
    public bool IsFailure => !IsSuccess && !IsNotModified;

    public ProjectConfig Config { get; }
    public RefreshErrorCode ErrorCode { get; }
    public object? ErrorMessage => !IsNotModified ? this.errorMessageOrToken : null;
    public Exception? ErrorException { get; }
}
