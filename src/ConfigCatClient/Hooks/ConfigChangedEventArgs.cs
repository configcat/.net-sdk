using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Provides data for the <see cref="ConfigCatClient.ConfigChanged"/> event.
    /// </summary>
    public class ConfigChangedEventArgs : EventArgs
    {
        internal ConfigChangedEventArgs(ProjectConfig newConfig)
        {
            NewConfig = newConfig;
        }

        /// <summary>
        /// The new <see cref="ProjectConfig"/> object.
        /// </summary>
        public ProjectConfig NewConfig { get; }
    }
}
