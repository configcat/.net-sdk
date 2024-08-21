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
    public FetchRequest(Uri uri, string? lastETag, KeyValuePair<string, string> sdkInfoHeader, TimeSpan timeout)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        LastETag = lastETag;
        SdkInfoHeader = sdkInfoHeader;
        Timeout = timeout;
    }

    /// <summary>
    /// The URI of the config.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The value of the <c>ETag</c> HTTP response header received during the last successful request (if any).
    /// </summary>
    public string? LastETag { get; }

    /// <summary>
    /// The name and value of the HTTP request header containing information about the SDK. Should be included in every request.
    /// </summary>
    public KeyValuePair<string, string> SdkInfoHeader { get; }

    /// <summary>
    /// The request timeout to apply, configured via <see cref="ConfigCatClientOptions.HttpTimeout"/>.
    /// </summary>
    public TimeSpan Timeout { get; }
}
