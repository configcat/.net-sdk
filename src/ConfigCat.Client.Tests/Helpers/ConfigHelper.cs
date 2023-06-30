using System;
using System.IO;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigHelper
{
    public static ProjectConfig FromString(string configJson, string? httpETag, DateTime timeStamp)
    {
        return new ProjectConfig(configJson, configJson.Deserialize<Config>(), timeStamp, httpETag);
    }

    public static ProjectConfig FromFile(string configJsonFilePath, string? httpETag, DateTime timeStamp)
    {
        return FromString(File.ReadAllText(configJsonFilePath), httpETag, timeStamp);
    }

    public static string GetSampleJson(string fileName)
    {
        using Stream stream = File.OpenRead(Path.Combine("data", fileName));
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
