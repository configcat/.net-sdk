using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private readonly ILogger logger;
        private readonly JsonSerializer serializer;

        public ConfigDeserializer(ILogger logger, JsonSerializer serializer)
        {
            this.logger = logger;
            this.serializer = serializer;
        }

        public bool TryDeserialize(ProjectConfig projectConfig, out IDictionary<string, Setting> settings)
        {
            if (projectConfig.JsonString == null)
            {
                this.logger.Warning("ConfigJson is not present.");

                settings = null;

                return false;
            }

            using (var stringReader = new StringReader(projectConfig.JsonString))
            using (var reader = new JsonTextReader(stringReader))
            {
                var settingsWithPreferences = this.serializer.Deserialize<SettingsWithPreferences>(reader);

                settings = settingsWithPreferences.Settings;

                return true;
            }
        }
    }
}
