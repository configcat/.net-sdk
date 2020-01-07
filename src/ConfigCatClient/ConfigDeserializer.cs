using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private readonly ILogger logger;

        public ConfigDeserializer(ILogger logger)
        {
            this.logger = logger;
        }

        public bool TryDeserialize(ProjectConfig projectConfig, out ConfigJson settings)
        {
            if (projectConfig.JsonString == null)
            {
                this.logger.Warning("ConfigJson is not present.");
                settings = null;
                return false;
            }

            settings = JsonConvert.DeserializeObject<ConfigJson>(projectConfig.JsonString);
            return true;
        }
    }
}
