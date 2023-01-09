using System;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

internal class ConfigDeserializer : IConfigDeserializer
{
    private SettingsWithPreferences lastDeserializedSettings;
    private string lastConfig;
    private string lastHttpETag;

    public bool TryDeserialize(string config, string httpETag, out SettingsWithPreferences settings)
    {
        if (config is null)
        {
            settings = null;
            return false;
        }

        var configContentHasChanged = !ProjectConfig.ContentEquals(this.lastHttpETag, this.lastConfig, httpETag, config);

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
