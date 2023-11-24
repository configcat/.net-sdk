#if USE_NEWTONSOFT_JSON
using System.IO;
using Newtonsoft.Json;
#else
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace System;

internal static class SerializationExtensions
{
#if USE_NEWTONSOFT_JSON
    private static readonly JsonSerializer Serializer = JsonSerializer.Create();
#else
    private static readonly JsonSerializerOptions TolerantSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
#endif

    public static T? Deserialize<T>(this string json, bool tolerant = false) => json.AsMemory().Deserialize<T>(tolerant);

    // NOTE: It would be better to use ReadOnlySpan<char>, however when the full string is wrapped in a span, json.ToString() result in a copy of the string.
    // This is not the case with ReadOnlyMemory<char>, so we use that until support for .NET 4.5 support is dropped.
    public static T? Deserialize<T>(this ReadOnlyMemory<char> json, bool tolerant = false)
    {
#if USE_NEWTONSOFT_JSON
        using var stringReader = new StringReader(json.ToString());
        using var reader = new JsonTextReader(stringReader);
        return Serializer.Deserialize<T>(reader);
#else
        return JsonSerializer.Deserialize<T>(json.Span, tolerant ? TolerantSerializerOptions : null);
#endif
    }

    public static T? DeserializeOrDefault<T>(this string json, bool tolerant = false) => json.AsMemory().DeserializeOrDefault<T>(tolerant);

    public static T? DeserializeOrDefault<T>(this ReadOnlyMemory<char> json, bool tolerant = false)
    {
        try
        {
            return json.Deserialize<T>(tolerant);
        }
        catch
        {
            return default;
        }
    }

    public static string Serialize<T>(this T objectToSerialize)
    {
#if USE_NEWTONSOFT_JSON
        return JsonConvert.SerializeObject(objectToSerialize);
#else
        return JsonSerializer.Serialize(objectToSerialize, TolerantSerializerOptions);
#endif
    }
}
