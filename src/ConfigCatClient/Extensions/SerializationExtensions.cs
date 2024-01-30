#if USE_NEWTONSOFT_JSON
using System.IO;
using Newtonsoft.Json;
#else
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    public static string Serialize<T>(this T objectToSerialize, bool unescapeAstral = false)
    {
#if USE_NEWTONSOFT_JSON
        return JsonConvert.SerializeObject(objectToSerialize);
#else
        var json = JsonSerializer.Serialize(objectToSerialize, TolerantSerializerOptions);
        if (unescapeAstral)
        {
            // NOTE: There's no easy way to configure System.Text.Json not to encode surrogate pairs (i.e. Unicode code points above U+FFFF).
            // The only way of doing it during serialization (https://github.com/dotnet/runtime/issues/54193#issuecomment-861155179) needs unsafe code,
            // which we want to avoid in this project. So, we resort to the following regex-based workaround:
            json = Regex.Replace(json, @"\\u[dD][89abAB][0-9a-fA-F]{2}\\u[dD][c-fC-F][0-9a-fA-F]{2}", match =>
            {
                // Ignore possible matches that aren't really escaped ('\\uD800\uDC00', '\\\\uD800\uDC00', etc.)
                var isEscaped = true;
                for (var i = match.Index - 1; i >= 0; i--)
                {
                    if (json[i] != '\\')
                    {
                        break;
                    }
                    isEscaped = !isEscaped;
                }
                if (!isEscaped)
                {
                    return match.Value;
                }

                var highSurrogate = ushort.Parse(match.Value.AsSpan(2, 4).ToParsable(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                var lowSurrogate = ushort.Parse(match.Value.AsSpan(8, 4).ToParsable(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                return char.ConvertFromUtf32(char.ConvertToUtf32((char)highSurrogate, (char)lowSurrogate));
            });
        }
        return json;
#endif
    }
}
