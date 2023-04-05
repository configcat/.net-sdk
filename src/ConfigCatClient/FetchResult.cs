using System;
using System.Diagnostics.CodeAnalysis;

namespace ConfigCat.Client;

internal readonly record struct FetchResult
{
    private static readonly object NotModifiedToken = new();

    public static FetchResult Success(ProjectConfig config)
    {
        return new FetchResult(config ?? throw new ArgumentNullException(nameof(config)), errorMessageOrToken: null);
    }

    public static FetchResult NotModified(ProjectConfig config)
    {
        return new FetchResult(config ?? throw new ArgumentNullException(nameof(config)), NotModifiedToken);
    }

    public static FetchResult Failure(ProjectConfig config, string errorMessage, Exception? errorException = null)
    {
        return new FetchResult(
            config ?? throw new ArgumentNullException(nameof(config)),
            errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
            errorException);
    }

    private readonly object? errorMessageOrToken;

    private FetchResult(ProjectConfig config, object? errorMessageOrToken, Exception? errorException = null)
    {
        Config = config;
        this.errorMessageOrToken = errorMessageOrToken;
        ErrorException = errorException;
    }

    public bool IsSuccess => this.errorMessageOrToken is null;
    public bool IsNotModified => ReferenceEquals(this.errorMessageOrToken, NotModifiedToken);
    [MemberNotNullWhen(true, nameof(ErrorMessage))]
    public bool IsFailure => this.errorMessageOrToken is string;

    public ProjectConfig Config { get; }
    public string? ErrorMessage => this.errorMessageOrToken as string;
    public Exception? ErrorException { get; }
}
