using System;

namespace ConfigCat.Client;

/// <summary>
/// Provides data for the <see cref="ConfigCatClient.ConfigFetched"/> event.
/// </summary>
public class ConfigFetchedEventArgs : EventArgs
{
    internal ConfigFetchedEventArgs(RefreshResult result, bool isInitiatedByUser)
    {
        Result = result;
        IsInitiatedByUser = isInitiatedByUser;
    }

    /// <summary>
    /// The result of the operation.
    /// </summary>
    public RefreshResult Result { get; }

    /// <summary>
    /// Indicates whether the operation was initiated by the user or by the SDK.
    /// </summary>
    public bool IsInitiatedByUser { get; }
}
