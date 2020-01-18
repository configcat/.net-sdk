using Newtonsoft.Json;
using System.Collections.Generic;

namespace ConfigCat.Client.Evaluate
{
    /// <summary>
    /// Represents the overalll structure of the config_v4.json files.
    /// </summary>
    internal class ConfigJson
    {
        /// <summary>
        /// Contains the Settings which are defined by the User, normally through the Dashboard App.
        /// </summary>
        [JsonProperty("userSpaceSettings")]
        internal Dictionary<string, Setting> UserSpaceSettings;

        /// <summary>
        /// Contains the Settings which are defined by the ConfigCat service.
        /// These Settings are used by the SDK to determine how it should work.
        /// </summary>
        [JsonProperty( "serviceSpaceSettings" )]
        internal ServiceSettings ServiceSpaceSettings;

        internal class ServiceSettings
        {
            [JsonProperty( "cdnServerNames" )]
            internal string CdnServerNamesCsv;
        }
    }
}
