using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

internal interface IConfigDeserializer
{
    bool TryDeserialize(string? config, string? httpETag, [NotNullWhen(true)] out SettingsWithPreferences? settings);
}
