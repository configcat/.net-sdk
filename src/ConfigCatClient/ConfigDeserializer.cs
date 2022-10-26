using ConfigCat.Client.Evaluation;
using System;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private SettingsWithPreferences lastDeserializedSettings;
        private string lastConfig;
        private string lastHttpETag;

        public bool TryDeserialize(string config, string httpETag, out SettingsWithPreferences settings)
        {
            if (config == null)
            {
                settings = null;
                return false;
            }

            var configContentHasChanged = this.lastHttpETag is not null && httpETag is not null
                ? this.lastHttpETag != httpETag
                : this.lastConfig != config;

            if (!configContentHasChanged)
            {
                settings = this.lastDeserializedSettings;
                return true;
            }

            this.lastDeserializedSettings = settings = config.Deserialize<SettingsWithPreferences>();
            this.lastConfig = config;
            this.lastHttpETag = httpETag;
            return true;
        }
    }
}
