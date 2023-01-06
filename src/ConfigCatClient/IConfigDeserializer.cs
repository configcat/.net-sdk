using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

internal interface IConfigDeserializer
{
    bool TryDeserialize(string config, string httpETag, out SettingsWithPreferences settings);
}
