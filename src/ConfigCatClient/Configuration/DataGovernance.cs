namespace ConfigCat.Client
{
    /// <summary>
    /// Control the location of the config.json files containing your feature flags and settings within the ConfigCat CDN.
    /// </summary>
    public enum DataGovernance : byte
    {
        /// <summary>
        /// Select this if your feature flags are published to all global CDN nodes.
        /// </summary>
        Global = 1,
        /// <summary>
        /// Select this if your feature flags are published to CDN nodes only in the EU.
        /// </summary>
        EuOnly = 2
    }
}
