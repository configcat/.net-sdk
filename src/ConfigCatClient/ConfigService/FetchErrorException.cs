using System;
using System.Net;

namespace ConfigCat.Client;

/// <summary>
/// Represents errors that can occur during a ConfigCat config fetch operation.
/// </summary>
public abstract class FetchErrorException : Exception
{
    /// <summary>
    /// Creates an instance of the <see cref="FetchErrorException"/> struct which indicates that the operation timed out.
    /// </summary>
    public static FetchErrorException Timeout(TimeSpan timeout, Exception? innerException = null)
        => new Timeout_($"Request timed out. Timeout value: {timeout}", timeout, innerException);

    /// <summary>
    /// Creates an instance of the <see cref="FetchErrorException"/> struct which indicates that the operation failed due to a network or protocol error.
    /// </summary>
    public static FetchErrorException Failure(WebExceptionStatus? status, Exception? innerException = null)
        => new Failure_("Request failed due to a network or protocol error.", status, innerException);

    private FetchErrorException(string message, Exception? innerException)
        : base(message, innerException) { }

    internal sealed class Timeout_ : FetchErrorException
    {
        public Timeout_(string message, TimeSpan timeout, Exception? innerException)
            : base(message, innerException)
        {
            Timeout = timeout;
        }

        public new TimeSpan Timeout { get; }
    }

    internal sealed class Failure_ : FetchErrorException
    {
        public Failure_(string message, WebExceptionStatus? status, Exception? innerException)
            : base(message, innerException)
        {
            Status = status;
        }

        public WebExceptionStatus? Status { get; }
    }
}
