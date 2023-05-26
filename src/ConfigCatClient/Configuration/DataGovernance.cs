namespace ConfigCat.Client;

/// <summary>
/// Controls the location of the config JSON files containing your feature flags and settings within the ConfigCat CDN.
/// </summary>
public enum DataGovernance : byte
{
    /// <summary>
    /// Choose this option if your config JSON files are published to all global CDN nodes.
    /// </summary>
    Global = 0,
    /// <summary>
    /// Choose this option if your config JSON files are published to CDN nodes only in the EU.
    /// </summary>
    EuOnly = 1
}
