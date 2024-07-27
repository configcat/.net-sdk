using System.Net;

namespace ConfigCat.Client;

/// <summary>
/// The response data of a ConfigCat config fetch operation.
/// </summary>
public readonly struct FetchResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FetchResponse"/> struct.
    /// </summary>
    public FetchResponse(HttpStatusCode statusCode, string? reasonPhrase, string? eTag, string? body)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        ETag = eTag;
        Body = body;
    }

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The HTTP reason phrase.
    /// </summary>
    public string? ReasonPhrase { get; }

    /// <summary>
    /// The value of the <c>ETag</c> HTTP response header.
    /// </summary>
    public string? ETag { get; }

    /// <summary>
    /// The response body.
    /// </summary>
    public string? Body { get; }

    /// <summary>
    /// Indicates whether the response is expected or not.
    /// </summary>
    public bool IsExpected => StatusCode is
        HttpStatusCode.OK
        or HttpStatusCode.NotModified
        or HttpStatusCode.Forbidden
        or HttpStatusCode.NotFound;
}
