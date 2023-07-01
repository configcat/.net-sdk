using System.IO;

namespace ConfigCat.Client.Tests.Helpers;

internal static class ConfigHelper
{
    public static string GetSampleJson(string fileName)
    {
        using Stream stream = File.OpenRead(Path.Combine("data", fileName));
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
