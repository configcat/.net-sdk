using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ConfigCat.Client;

/// <summary>
/// The response data of a ConfigCat config fetch operation.
/// </summary>
public readonly struct FetchResponse
{
    internal static FetchResponse From(HttpResponseMessage httpResponse, string? httpResponseBody = null)
    {
        return new FetchResponse(httpResponse.StatusCode, httpResponse.ReasonPhrase, httpResponse.Headers, httpResponseBody);
    }

    private readonly object? headersOrETag; // either null or a string or HttpResponseHeaders

    private FetchResponse(HttpStatusCode statusCode, string? reasonPhrase, object? headersOrETag, string? body)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        this.headersOrETag = headersOrETag;
        Body = body;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FetchResponse"/> struct.
    /// </summary>
    public FetchResponse(HttpStatusCode statusCode, string? reasonPhrase, string? eTag, string? body)
        : this(statusCode, reasonPhrase, (object?)eTag, body) { }

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
    public string? ETag => this.headersOrETag is HttpResponseHeaders headers ? headers.ETag?.Tag : (string?)this.headersOrETag;

    internal string? RayId => this.headersOrETag is HttpResponseHeaders headers && headers.TryGetValues("CF-RAY", out var values) ? values.FirstOrDefault() : null;

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
