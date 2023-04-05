using System;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

internal sealed class ConfigDeserializer : IConfigDeserializer
{
    private SettingsWithPreferences? lastDeserializedSettings;
    private string? lastConfig;
    private string? lastHttpETag;

    public bool TryDeserialize(string? config, string? httpETag, [NotNullWhen(true)] out SettingsWithPreferences? settings)
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
            return settings is not null;
        }

        this.lastDeserializedSettings = settings = config.Deserialize<SettingsWithPreferences>()
            ?? throw new InvalidOperationException("Invalid config JSON content: " + config);
        this.lastConfig = config;
        this.lastHttpETag = httpETag;
        return true;
    }
}
