using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private readonly ILogger logger;

        public ConfigDeserializer(ILogger logger)
        {
            this.logger = logger;
        }

        public bool TryDeserialize(ProjectConfig projectConfig, out IDictionary<string, Setting> settings)
        {
            if (projectConfig.JsonString == null)
            {
                this.logger.Warning("ConfigJson is not present.");
                
                settings = null;

                return false;
            }

            var settingsWithPreferences = JsonConvert.DeserializeObject<SettingsWithPreferences>(projectConfig.JsonString);

            settings = settingsWithPreferences.Settings;

            return true;
        }
    }
}
