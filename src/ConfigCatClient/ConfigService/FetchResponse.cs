using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace ConfigCat.Client;

/// <summary>
/// The response data of a ConfigCat config fetch operation.
/// </summary>
public readonly struct FetchResponse
{
    private FetchResponse(HttpStatusCode statusCode, string? reasonPhrase, string? body)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Body = body;
    }

    internal FetchResponse(HttpResponseMessage httpResponse, string? httpResponseBody = null)
        : this(httpResponse.StatusCode, httpResponse.ReasonPhrase, httpResponseBody)
    {
        ETag = httpResponse.Headers.ETag?.ToString(); // NOTE: ToString() is necessary because the "W/" prefix must be included!
        RayId = httpResponse.Headers.TryGetValues("CF-RAY", out var values) ? values.FirstOrDefault() : null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FetchResponse"/> struct.
    /// </summary>
    public FetchResponse(HttpStatusCode statusCode, string? reasonPhrase, IEnumerable<KeyValuePair<string, string>> headers, string? body = null)
        : this(statusCode, reasonPhrase, body)
    {
        string? eTag = null, rayId = null;

        foreach (var header in headers)
        {
            if (eTag is null && "ETag".Equals(header.Key, StringComparison.OrdinalIgnoreCase))
            {
                eTag = header.Value;
                if (rayId is not null)
                {
                    break;
                }
            }
            else if (rayId is null && "CF-RAY".Equals(header.Key, StringComparison.OrdinalIgnoreCase))
            {
                rayId = header.Value;
                if (eTag is not null)
                {
                    break;
                }
            }
        }

        ETag = eTag;
        RayId = rayId;
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

    internal string? RayId { get; }

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
