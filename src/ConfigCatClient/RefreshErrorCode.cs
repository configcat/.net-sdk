namespace ConfigCat.Client;

/// <summary>
/// Specifies the possible config data refresh error codes.
/// </summary>
public enum RefreshErrorCode
{
    /// <summary>
    /// An unexpected error occurred during the refresh operation.
    /// </summary>
    UnexpectedError = -1,
    /// <summary>
    /// No error occurred (the refresh operation was successful).
    /// </summary>
    None = 0,
    /// <summary>
    /// The refresh operation failed because the client is configured to use the <see cref="OverrideBehaviour.LocalOnly"/> override behavior,
    /// which prevents making HTTP requests.
    /// </summary>
    LocalOnlyClient = 1,
    /// <summary>
    /// The refresh operation failed because the client is in offline mode, it cannot initiate HTTP requests.
    /// </summary>
    OfflineClient = 3200,
    /// <summary>
    /// The refresh operation failed because a HTTP response indicating an invalid SDK Key was received (403 Forbidden or 404 Not Found).
    /// </summary>
    InvalidSdkKey = 1100,
    /// <summary>
    /// The refresh operation failed because an invalid HTTP response was received (unexpected HTTP status code).
    /// </summary>
    UnexpectedHttpResponse = 1101,
    /// <summary>
    /// The refresh operation failed because the HTTP request timed out.
    /// </summary>
    HttpRequestTimeout = 1102,
    /// <summary>
    /// The refresh operation failed because the HTTP request failed (most likely, due to a local network issue).
    /// </summary>
    HttpRequestFailure = 1103,
    /// <summary>
    /// The refresh operation failed because an invalid HTTP response was received (200 OK with an invalid content).
    /// </summary>
    InvalidHttpResponseContent = 1105,
    /// <summary>
    /// The refresh operation failed because an invalid HTTP response was received (304 Not Modified when no config JSON was cached locally).
    /// </summary>
    InvalidHttpResponseWhenLocalCacheIsEmpty = 1106,
}
