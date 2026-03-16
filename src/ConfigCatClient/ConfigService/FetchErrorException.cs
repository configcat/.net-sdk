using System;
using System.Net;

namespace ConfigCat.Client;

/// <summary>
/// Represents errors that can occur during a ConfigCat config fetch operation.
/// </summary>
public abstract class FetchErrorException : Exception
{
    /// <summary>
    /// Creates an instance of the <see cref="FetchErrorException"/> class which indicates that the operation timed out.
    /// </summary>
    public static FetchErrorException Timeout(TimeSpan timeout, Exception? innerException = null)
        => new Timeout_(timeout, innerException, rayId: null);

    /// <summary>
    /// Creates an instance of the <see cref="FetchErrorException"/> class which indicates that the operation failed due to a network or protocol error.
    /// </summary>
    public static FetchErrorException Failure(WebExceptionStatus? status, Exception? innerException = null)
        => new Failure_(status, innerException, rayId: null);

    private FetchErrorException(string message, Exception? innerException, string? rayId)
        : base(message, innerException)
    {
        RayId = rayId;
    }

    internal string? RayId { get; }

    internal sealed class Timeout_ : FetchErrorException
    {
        public Timeout_(TimeSpan timeout, Exception? innerException, string? rayId)
            : base($"Request timed out. Timeout value: {timeout}", innerException, rayId)
        {
            Timeout = timeout;
        }

        public new TimeSpan Timeout { get; }
    }

    internal sealed class Failure_ : FetchErrorException
    {
        public Failure_(WebExceptionStatus? status, Exception? innerException, string? rayId)
            : base("Request failed due to a network or protocol error.", innerException, rayId)
        {
            Status = status;
        }

        public WebExceptionStatus? Status { get; }
    }
}
