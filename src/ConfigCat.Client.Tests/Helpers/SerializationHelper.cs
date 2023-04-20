using System;
using System.IO;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client.Tests.Helpers;

internal static class SerializationHelper
{
    public static ProjectConfig ProjectConfigFromString(string configJson, string? httpETag, DateTime timeStamp)
    {
        return new ProjectConfig(configJson.Deserialize<SettingsWithPreferences>(), timeStamp, httpETag);
    }

    public static ProjectConfig ProjectConfigFromFile(string configJsonFilePath, string? httpETag, DateTime timeStamp)
    {
        return ProjectConfigFromString(File.ReadAllText(configJsonFilePath), httpETag, timeStamp);
    }
}
