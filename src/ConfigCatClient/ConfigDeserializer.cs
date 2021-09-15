using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;
using System.IO;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private readonly ILogger logger;
        private readonly JsonSerializer serializer;
        private int hash;
        private SettingsWithPreferences lastSerializedSettings;

        public ConfigDeserializer(ILogger logger, JsonSerializer serializer)
        {
            this.logger = logger;
            this.serializer = serializer;
        }

        public bool TryDeserialize(string config, out SettingsWithPreferences settings)
        {
            if (config == null)
            {
                this.logger.Warning("ConfigJson is not present.");

                settings = null;

                return false;
            }

            var hashCode = config.GetHashCode();
            if(this.hash == hashCode)
            {
                settings = this.lastSerializedSettings;
                return true;
            }

            using (var stringReader = new StringReader(config))
            using (var reader = new JsonTextReader(stringReader))
            { 
                this.lastSerializedSettings = settings = this.serializer.Deserialize<SettingsWithPreferences>(reader);
            }

            this.hash = hashCode;
            return true;
        }
    }
}
