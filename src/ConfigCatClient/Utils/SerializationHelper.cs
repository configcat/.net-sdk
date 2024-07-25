using System;
using ConfigCat.Client.Override;

#if USE_NEWTONSOFT_JSON
using System.IO;
using Newtonsoft.Json;
#else
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
#endif

namespace ConfigCat.Client.Utils;

#if !USE_NEWTONSOFT_JSON
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(LocalFileDataSource.SimplifiedConfig))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, JsonNode>))]
internal partial class SourceGenSerializationContext : JsonSerializerContext
{
    // Implemented by System.Text.Json source generator.
    // See also:
    // * https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
    // * https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
}
#endif

internal static partial class SerializationHelper
{
#if USE_NEWTONSOFT_JSON
    private static readonly JsonSerializer Serializer = JsonSerializer.Create();

    private static T? Deserialize<T>(ReadOnlyMemory<char> json)
    {
        using var stringReader = new StringReader(json.ToString());
        using var reader = new JsonTextReader(stringReader);
        return Serializer.Deserialize<T>(reader);
    }
#else
    private static readonly SourceGenSerializationContext TolerantSerializationContext = new SourceGenSerializationContext(new JsonSerializerOptions
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
#endif

    // NOTE: It would be better to use ReadOnlySpan<char>, however when the full string is wrapped in a span, json.ToString() result in a copy of the string.
    // This is not the case with ReadOnlyMemory<char>, so we use that until support for .NET 4.5 support is dropped.

    public static Config? DeserializeConfig(ReadOnlyMemory<char> json, bool tolerant = false, bool throwOnError = true)
    {
        try
        {
#if USE_NEWTONSOFT_JSON
            return Deserialize<Config>(json);
#else
            return JsonSerializer.Deserialize(json.Span, tolerant ? TolerantSerializationContext.Config : SourceGenSerializationContext.Default.Config);
#endif
        }
        catch when (!throwOnError)
        {
            return default;
        }
    }

    public static LocalFileDataSource.SimplifiedConfig? DeserializeSimplifiedConfig(ReadOnlyMemory<char> json, bool tolerant = false, bool throwOnError = true)
    {
        try
        {
#if USE_NEWTONSOFT_JSON
            return Deserialize<LocalFileDataSource.SimplifiedConfig>(json);
#else
            return JsonSerializer.Deserialize(json.Span, tolerant ? TolerantSerializationContext.SimplifiedConfig : SourceGenSerializationContext.Default.SimplifiedConfig);
#endif
        }
        catch when (!throwOnError)
        {
            return default;
        }
    }

    public static string[]? DeserializeStringArray(ReadOnlyMemory<char> json, bool tolerant = false, bool throwOnError = true)
    {
        try
        {
#if USE_NEWTONSOFT_JSON
            return Deserialize<string[]>(json);
#else
            return JsonSerializer.Deserialize(json.Span, tolerant ? TolerantSerializationContext.StringArray : SourceGenSerializationContext.Default.StringArray);
#endif
        }
        catch when (!throwOnError)
        {
            return default;
        }
    }

    public static string SerializeStringArray(string[] obj, bool unescapeAstral = false)
    {
#if USE_NEWTONSOFT_JSON
        return JsonConvert.SerializeObject(obj);
#else
        var json = JsonSerializer.Serialize(obj, TolerantSerializationContext.StringArray);
        return unescapeAstral ? UnescapeAstralCodePoints(json) : json;
#endif
    }

    public static string SerializeUser(User obj, bool unescapeAstral = false)
    {
#if USE_NEWTONSOFT_JSON
        return JsonConvert.SerializeObject(obj.GetAllAttributes());
#else
        // NOTE: When using System.Text.Json source generation, polymorphic types can't be serialized unless
        // all the possible concrete type are listed using JsonSerializableAttribute.
        // However, we allow consumers to pass values of any type in custom user attributes, so obviously
        // there is no way to list all the possible types. As a best effort, we can approximate the output of
        // the non-source generated serialization by building a JSON DOM and serializing that.
        var attributes = obj.GetAllAttributes(value =>
        {
            HashSet<object>? visitedCollections = null;
            return UnknownValueToJsonNode(value, ref visitedCollections);
        });

        var json = JsonSerializer.Serialize(attributes!, TolerantSerializationContext.DictionaryStringJsonNode);
        return unescapeAstral ? UnescapeAstralCodePoints(json) : json;
#endif
    }

#if !USE_NEWTONSOFT_JSON
    private static JsonNode? UnknownValueToJsonNode(object? value, ref HashSet<object>? visitedCollections)
    {
        if (value is null)
        {
            return null;
        }

        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Boolean: return JsonValue.Create((bool)value);
            case TypeCode.Char: return JsonValue.Create((char)value);
            case TypeCode.SByte: return JsonValue.Create((sbyte)value);
            case TypeCode.Byte: return JsonValue.Create((byte)value);
            case TypeCode.Int16: return JsonValue.Create((short)value);
            case TypeCode.UInt16: return JsonValue.Create((ushort)value);
            case TypeCode.Int32: return JsonValue.Create((int)value);
            case TypeCode.UInt32: return JsonValue.Create((uint)value);
            case TypeCode.Int64: return JsonValue.Create((long)value);
            case TypeCode.UInt64: return JsonValue.Create((ulong)value);
#if NETCOREAPP
            case TypeCode.Single: return JsonValue.Create((float)value);
            case TypeCode.Double: return JsonValue.Create((double)value);
#else
            // On .NET Framework, System.Text.Json serializes float and double values incorrectly.
            // E.g. 3.14 -> 3.1400000000000001. We can workaround this by casting such values to decimal.
            case TypeCode.Single: return JsonValue.Create((decimal)(float)value);
            case TypeCode.Double: return JsonValue.Create((decimal)(double)value);
#endif
            case TypeCode.Decimal: return JsonValue.Create((decimal)value);
            case TypeCode.DateTime: return JsonValue.Create((DateTime)value);
            case TypeCode.String: return JsonValue.Create((string)value);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return JsonValue.Create(dateTimeOffset);
        }

        if (value is Guid guid)
        {
            return JsonValue.Create(guid);
        }

        if (value is IEnumerable enumerable)
        {
            visitedCollections ??= new HashSet<object>();
            if (!visitedCollections.Add(enumerable))
            {
                // NOTE: We need to check for circular references because that would result in a StackOverflowException, which would bring down the process.
                throw new InvalidOperationException("A circular reference was detected in the serialized object graph.");
            }

            JsonNode jsonNode;
            if (value is IDictionary dictionary)
            {
                var jsonObject = new JsonObject();
                var enumerator = dictionary.GetEnumerator();
                using (enumerator as IDisposable)
                {
                    while (enumerator.MoveNext())
                    {
                        var entry = enumerator.Entry;
                        jsonObject.Add(entry.Key?.ToString() ?? "", UnknownValueToJsonNode(entry.Value, ref visitedCollections));
                    }
                }
                jsonNode = jsonObject;
            }
            else
            {
                var jsonArray = new JsonArray();
                foreach (var item in enumerable)
                {
                    jsonArray.Add(UnknownValueToJsonNode(item, ref visitedCollections));
                }
                jsonNode = jsonArray;
            }

            visitedCollections!.Remove(enumerable);
            return jsonNode;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\\u[dD][89abAB][0-9a-fA-F]{2}\\u[dD][c-fC-F][0-9a-fA-F]{2}", RegexOptions.CultureInvariant, 5000)]
    private static partial Regex EscapedSurrogatePairsRegex();
#else
    private static readonly Regex EscapedSurrogatePairsRegexCached = new Regex(@"\\u[dD][89abAB][0-9a-fA-F]{2}\\u[dD][c-fC-F][0-9a-fA-F]{2}", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
    private static Regex EscapedSurrogatePairsRegex() => EscapedSurrogatePairsRegexCached;
#endif

    private static string UnescapeAstralCodePoints(string json)
    {
        // NOTE: There's no easy way to configure System.Text.Json not to encode surrogate pairs (i.e. Unicode code points above U+FFFF).
        // The only way of doing it during serialization (https://github.com/dotnet/runtime/issues/54193#issuecomment-861155179) needs unsafe code,
        // which we want to avoid in this project. So, we resort to the following regex-based workaround:
        return EscapedSurrogatePairsRegex().Replace(json, match =>
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
#endif
        }
