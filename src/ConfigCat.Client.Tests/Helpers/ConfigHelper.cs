using System;
using System.IO;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigHelper
{
    public static ProjectConfig FromString(string configJson, string? httpETag, DateTime timeStamp)
    {
        return new ProjectConfig(configJson, configJson.Deserialize<SettingsWithPreferences>(), timeStamp, httpETag);
    }

    public static ProjectConfig FromFile(string configJsonFilePath, string? httpETag, DateTime timeStamp)
    {
        return FromString(File.ReadAllText(configJsonFilePath), httpETag, timeStamp);
    }
}
