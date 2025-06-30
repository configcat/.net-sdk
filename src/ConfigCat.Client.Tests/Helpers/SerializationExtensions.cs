using System.Text.Encodings.Web;
using System.Text.Json;

namespace System;

internal static class SerializationExtensions
{
    private static readonly JsonSerializerOptions SerializationOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static T? Deserialize<T>(this ReadOnlySpan<char> json)
    {
        return JsonSerializer.Deserialize<T>(json);
    }

    public static string Serialize<T>(this T objectToSerialize)
    {
        return JsonSerializer.Serialize(objectToSerialize, SerializationOptions);
    }
}
