using System;
using System.Collections.Generic;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client;

/// <summary>
/// The request parameters for a ConfigCat config fetch operation.
/// </summary>
public readonly struct FetchRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FetchRequest"/> struct.
    /// </summary>
    public FetchRequest(Uri uri, string? lastETag, IReadOnlyList<KeyValuePair<string, string>> headers, TimeSpan timeout)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        LastETag = lastETag;
        Headers = headers;
        Timeout = timeout;
    }

    /// <summary>
    /// The URI of the config.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The value of the <c>ETag</c> HTTP response header received during the last successful request (if any).
    /// If available, should be included in the HTTP request, either in the <c>If-None-Match</c> header or in the <c>ccetag</c> query string parameter.
    /// </summary>
    /// <remarks>
    /// In browser runtime environments the <c>If-None-Match</c> header should be avoided because that may cause unnecessary CORS preflight requests.
    /// </remarks>
    public string? LastETag { get; }

    /// <summary>
    /// Additional HTTP request headers. Should be included in every HTTP request.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

    /// <summary>
    /// The request timeout to apply, configured via <see cref="ConfigCatClientOptions.HttpTimeout"/>.
    /// </summary>
    public TimeSpan Timeout { get; }
}
